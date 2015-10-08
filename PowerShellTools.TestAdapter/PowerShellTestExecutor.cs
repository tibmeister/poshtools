using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Microsoft.PowerShell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using PowerShellTools.Common.Logging;
using PowerShellTools.TestAdapter.Properties;

namespace PowerShellTools.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class PowerShellTestExecutor : ITestExecutor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (PowerShellTestExecutor));
        public void RunTests(IEnumerable<string> sources, IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            SetupExecutionPolicy();
            IEnumerable<TestCase> tests = PowerShellTestDiscoverer.GetTests(sources, null);
            RunTests(tests, runContext, frameworkHandle);
        }

        private static void SetupExecutionPolicy()
        {
            SetExecutionPolicy(ExecutionPolicy.RemoteSigned, ExecutionPolicyScope.Process);
        }

        private static void SetExecutionPolicy(ExecutionPolicy policy, ExecutionPolicyScope scope)
        {
            Log.DebugFormat("SetExecutionPolicy({0},{1})", policy, scope);
            ExecutionPolicy currentPolicy = ExecutionPolicy.Undefined;

            using (var ps = PowerShell.Create())
            {
                ps.AddCommand("Get-ExecutionPolicy");

                foreach (var result in ps.Invoke())
                {
                    currentPolicy = ((ExecutionPolicy)result.BaseObject);
                    break;
                }

                Log.DebugFormat("Current Policy: {0}", currentPolicy);

                if ((policy <= currentPolicy || currentPolicy == ExecutionPolicy.Bypass) && currentPolicy != ExecutionPolicy.Undefined) //Bypass is the absolute least restrictive, but as added in PS 2.0, and thus has a value of '4' instead of a value that corresponds to it's relative restrictiveness
                    return;

                ps.Commands.Clear();

                ps.AddCommand("Set-ExecutionPolicy").AddParameter("ExecutionPolicy", policy).AddParameter("Scope", scope).AddParameter("Force");
                ps.Invoke();
            }
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _mCancelled = false;
            SetupExecutionPolicy();
            foreach (var test in tests)
            {
                if (_mCancelled)
                {
                    Log.Debug("Test run canceled.");
                    break;
                }

                var testResult = new TestResult(test);
                testResult.Outcome = TestOutcome.Failed;
                testResult.ErrorMessage = Resources.UnexpectedError;

                PowerShellTestResult testResultData = null;
                var testOutput = new StringBuilder();

                try
                {
                    var testAdapter = new TestAdapterHost();
                    testAdapter.HostUi.OutputString = s => testOutput.Append(s);

                    var runpsace = RunspaceFactory.CreateRunspace(testAdapter);
                    runpsace.Open();

                    using (var ps = PowerShell.Create())
                    {
                        ps.Runspace = runpsace;
                        testResultData = RunTest(ps, test, runContext);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Test run threw an exception.", ex);
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = ex.Message;
                    testResult.ErrorStackTrace = ex.StackTrace;
                }

                if (testResultData != null)
                {
                    Log.DebugFormat("Test Result: {0}", testResultData);
                    testResult.Outcome = testResultData.Outcome;
                    testResult.ErrorMessage = testResultData.ErrorMessage;
                    testResult.ErrorStackTrace = testResultData.ErrorStacktrace;
                }

                if (testOutput.Length > 0)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, testOutput.ToString());
                }

                frameworkHandle.RecordResult(testResult);
            }
        }

        public void Cancel()
        {
            _mCancelled = true;
        }

        public const string ExecutorUriString = "executor://PowerShellTestExecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private bool _mCancelled;

        public PowerShellTestResult RunTest(PowerShell powerShell, TestCase testCase, IRunContext runContext)
        {
            var module = FindModule("Pester", runContext);
            powerShell.AddCommand("Import-Module").AddParameter("Name", module);
            powerShell.Invoke();
            powerShell.Commands.Clear();

            if (powerShell.HadErrors)
            {
                var errorRecord = powerShell.Streams.Error.FirstOrDefault();
                var errorMessage = errorRecord == null ? string.Empty : errorRecord.ToString();
                Log.WarnFormat("Failed to import Pester module. {0}", errorMessage);
                return new PowerShellTestResult(TestOutcome.Failed, Resources.FailedToLoadPesterModule + errorMessage, string.Empty);
            }

            var fi = new FileInfo(testCase.CodeFilePath);

            var describeName = testCase.FullyQualifiedName;

            Log.DebugFormat("Running test: {0} @ {1}", describeName, fi.FullName);

            powerShell.AddCommand("Invoke-Pester")
                .AddParameter("Path", fi.Directory.FullName)
                .AddParameter("TestName", describeName)
                .AddParameter("PassThru");

            var pesterResult = powerShell.Invoke().FirstOrDefault();

            if (pesterResult == null || powerShell.HadErrors)
            {
                var sb = new StringBuilder();
                foreach (var errorRecord in powerShell.Streams.Error)
                {
                    sb.AppendLine(errorRecord.ToString());
                }

                return new PowerShellTestResult(TestOutcome.Failed, sb.ToString());
            }

            var results = pesterResult.Properties["TestResult"].Value as Array;

            var testOutcome = TestOutcome.NotFound;

            var error = new StringBuilder();
            var stackTrace = new StringBuilder();

            foreach (PSObject result in results)
            {
                var describe = result.Properties["Describe"].Value as string;
                if (describeName.Equals(describe, StringComparison.OrdinalIgnoreCase))
                {
                    var currentOutcome = GetOutcome(result.Properties["Result"].Value as string);
                    if (currentOutcome == TestOutcome.Failed)
                    {
                        testOutcome = TestOutcome.Failed;
                    }
                    else if (testOutcome == TestOutcome.Passed && currentOutcome != TestOutcome.Passed)
                    {
                        testOutcome = currentOutcome;
                    }
                    else if (testOutcome == TestOutcome.NotFound)
                    {
                        testOutcome = currentOutcome;
                    }

                    var context = result.Properties["Context"].Value as string;
                    var name = result.Properties["Name"].Value as string;
                    var stackTraceString = result.Properties["StackTrace"].Value as string;
                    var errorString = result.Properties["FailureMessage"].Value as string;

                    Log.DebugFormat("Found test result for {0}", describeName);
                    Log.DebugFormat("Test outcome: {0}", testOutcome);


                    if (!string.IsNullOrEmpty(stackTraceString))
                    {
                        stackTrace.AppendLine(string.Format("{0} it {1}\r\n{2}", context, name, stackTraceString));
                    }

                    if (!string.IsNullOrEmpty(errorString))
                    {
                        error.AppendLine(string.Format("{0} it {1}\r\n{2}", context, name, errorString));
                    }
                }
            }

            return new PowerShellTestResult(testOutcome, error.ToString(), stackTrace.ToString());
        }

        private TestOutcome GetOutcome(string testResult)
        {
            if (string.IsNullOrEmpty(testResult))
            {
                return TestOutcome.NotFound;
            }

            if (testResult.Equals("passed", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.Passed;
            }
            if (testResult.Equals("skipped", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.Skipped;
            }
            if (testResult.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                return TestOutcome.Skipped;
            }
            return TestOutcome.Failed;
        }

        protected string FindModule(string moduleName, IRunContext runContext)
        {
            var pesterPath = GetModulePath(moduleName, runContext.TestRunDirectory);
            if (string.IsNullOrEmpty(pesterPath))
            {
                pesterPath = GetModulePath(moduleName, runContext.SolutionDirectory);
            }

            if (string.IsNullOrEmpty(pesterPath))
            {
                pesterPath = moduleName;
            }

            return pesterPath;
        }

        private static string GetModulePath(string moduleName, string root)
        {
            // Default packages path for nuget.
            var packagesRoot = Path.Combine(root, "packages");

            // TODO: Scour for custom nuget packages paths.

            if (Directory.Exists(packagesRoot))
            {
                var packagePath = Directory.GetDirectories(packagesRoot, moduleName + "*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (null != packagePath)
                {
                    var psd1 = Path.Combine(packagePath, string.Format(@"tools\{0}.psd1", moduleName));
                    if (File.Exists(psd1))
                    {
                        return psd1;
                    }

                    var psm1 = Path.Combine(packagePath, string.Format(@"tools\{0}.psm1", moduleName));
                    if (File.Exists(psm1))
                    {
                        return psm1;
                    }
                    var dll = Path.Combine(packagePath, string.Format(@"tools\{0}.dll", moduleName));
                    if (File.Exists(dll))
                    {
                        return dll;
                    }
                }
            }

            return null;
        }
    }

}
