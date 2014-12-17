using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace PowerShellTools.TestAdapter
{
    internal class PesterTestExecutor : PowerShellTestExecutorBase
    {
        public override string TestFramework
        {
            get { return "Pester"; }
        }

        public override PowerShellTestResult RunTest(PowerShell powerShell, TestCase testCase, IRunContext runContext)
        {
            var module = FindModule("Pester", runContext);
            powerShell.AddCommand("Import-Module").AddParameter("Name", module);
            powerShell.Invoke();

            powerShell.Commands.Clear();

            var fi = new FileInfo(testCase.CodeFilePath);

            var tempFile = Path.GetTempFileName();

            var describeName = testCase.FullyQualifiedName.Split(new[] {"||"}, StringSplitOptions.None)[1];
            var testCaseName = testCase.FullyQualifiedName.Split(new[] { "||" }, StringSplitOptions.None)[3];

            powerShell.AddCommand("Invoke-Pester")
                .AddParameter("relative_path", fi.Directory.FullName)
                .AddParameter("TestName", describeName)
                .AddParameter("OutputXml", tempFile);

            powerShell.Invoke();

            return ParseResultFile(tempFile, fi.Directory.FullName, describeName, testCaseName);
        }

        private PowerShellTestResult ParseResultFile(string file, string directory, string describeName, string testCaseName)
        {
            TestResultsEnum testResult;
            string error = string.Empty, stackTrace = string.Empty;

            using (var s = new FileStream(file, FileMode.Open))
            {
                var root = XDocument.Load(s).Root;

                if (root == null)
                {
                    return new PowerShellTestResult(TestOutcome.NotFound);
                }

                var suite =
                    root.Elements("test-suite")
                        .FirstOrDefault(
                            m => m.Attribute("name").Value.Equals(directory, StringComparison.OrdinalIgnoreCase));

                if (suite == null)
                {
                    return new PowerShellTestResult(TestOutcome.NotFound);
                }

                var describe =
                    suite.Descendants("test-suite")
                        .FirstOrDefault(
                            m =>
                                m.Attribute("name")
                                    .Value.Equals(describeName, StringComparison.OrdinalIgnoreCase));

                if (describe == null)
                {
                    return new PowerShellTestResult(TestOutcome.NotFound);
                }

                testResult = (TestResultsEnum)Enum.Parse(typeof(TestResultsEnum), describe.Attribute("result").Value);
                if (testResult != TestResultsEnum.Success)
                {
                    var sb = new StringBuilder();
                    foreach (var res in describe.Descendants("results"))
                    {
                        var testcase =
                            res.Elements("test-case")
                                .FirstOrDefault(
                                    m =>
                                        m.Attribute("name")
                                            .Value.Equals(testCaseName, StringComparison.OrdinalIgnoreCase));
                                
                            var name = testcase.Attribute("name").Value;
                            var result = testcase.Attribute("result").Value;

                            if (result != "Success")
                            {
                                var messageNode = testcase.Descendants("message").FirstOrDefault();
                                var stacktraceNode = testcase.Descendants("stack-trace").FirstOrDefault();

                                sb.AppendLine(String.Format("{1} [{0}]", name, result));
                                if (messageNode != null)
                                {
                                    sb.AppendLine(messageNode.Value);
                                }

                                if (stacktraceNode != null)
                                {
                                    stackTrace = stacktraceNode.Value;
                                }
                            }
                        }
                            

                    error = sb.ToString();
                }
            }

            File.Delete(file);
            return new PowerShellTestResult(GetOutcome(testResult), error, stackTrace);
        }

        private TestOutcome GetOutcome(TestResultsEnum testResult)
        {
            if (testResult == TestResultsEnum.Success)
            {
                return TestOutcome.Passed;
            }

            if (testResult == TestResultsEnum.Inconclusive)
            {
                return TestOutcome.None;
            }

            if (testResult == TestResultsEnum.Error | testResult == TestResultsEnum.Failure)
            {
                return TestOutcome.Failed;
            }

            if (testResult == TestResultsEnum.Ignored | testResult == TestResultsEnum.Skipped)
            {
                return TestOutcome.Skipped;
            }

            return TestOutcome.None;
        }
    }

    /// <summary>
    /// Test results
    /// </summary>
    public enum TestResultsEnum
    {
        Success,
        Failure,
        Inconclusive,
        Ignored,
        Skipped,
        Invalid,
        Error,
    }
}
