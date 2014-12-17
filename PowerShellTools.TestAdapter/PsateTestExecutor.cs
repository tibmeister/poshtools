using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace PowerShellTools.TestAdapter
{
    internal class PsateTestExecutor : PowerShellTestExecutorBase
    {
        public override string TestFramework
        {
            get { return "PSate"; }
        }

        public override PowerShellTestResult RunTest(PowerShell powerShell, TestCase testCase, IRunContext runContext)
        {
            var module = FindModule("PSate", runContext);
            powerShell.AddCommand("Import-Module").AddParameter("Name", module);
            powerShell.Invoke();

            powerShell.Commands.Clear();

            powerShell.AddCommand("Invoke-Tests")
                .AddParameter("Path", testCase.CodeFilePath)
                .AddParameter("Output", "Results")
                .AddParameter("ResultsVariable", "Results");

            powerShell.Invoke();

            powerShell.Commands.Clear();
            powerShell.AddCommand("Get-Variable").AddParameter("Name", "Results");
            var results = powerShell.Invoke<PSObject>();

            if (powerShell.HadErrors && (results == null || !results.Any()))
            {
                var errors = powerShell.Streams.Error;
                var sb = new StringBuilder();
                foreach (var error in errors)
                {
                    sb.AppendLine(error.ToString());
                }

                return new PowerShellTestResult(TestOutcome.Failed, sb.ToString(), errors.FirstOrDefault().ScriptStackTrace);
            }

            var testFixture = testCase.FullyQualifiedName.Split(new []{"||"}, StringSplitOptions.None)[1];
            var testCaseName = testCase.FullyQualifiedName.Split(new[] { "||" }, StringSplitOptions.None)[2];

            return ParseTestResult(results.FirstOrDefault(), testFixture, testCaseName);
        }

        public PowerShellTestResult ParseTestResult(PSObject obj,  string textFixtureName, string testCaseName)
        {
            if (obj == null)
            {
                return new PowerShellTestResult(TestOutcome.NotFound);
            }

            var variable = obj.BaseObject as PSVariable;
            if (variable == null)
            {
                throw new ArgumentException("Argument was not a variable!", "obj");
            }

            var hashTable = variable.Value as Hashtable;
            if (hashTable == null)
            {
                throw new ArgumentException("Argument was not a hashtable!", "obj");
            }

            hashTable = ((object[]) hashTable["Cases"])[0] as Hashtable; //File

            if (hashTable == null)
            {
                throw new ArgumentException("Hashtable did not contain the file cases!");
            }

            hashTable = ((object[])hashTable["Cases"]).FirstOrDefault(m => ((Hashtable)m)["Name"].ToString() == textFixtureName) as Hashtable; // TextFixture

            if (hashTable == null)
            {
                throw new ArgumentException("Hashtable did not contain the test fixture cases!");
            }

            hashTable = ((object[])hashTable["Cases"]).FirstOrDefault(m => ((Hashtable)m)["Name"].ToString() == testCaseName) as Hashtable; // TestCase

            if (hashTable == null)
            {
                throw new ArgumentException("Hashtable did not contain the test cases!");
            }

            var result = hashTable["Result"] as String;
            var exception = hashTable["Exception"] as ErrorRecord;
            var stackTrace = ((object[]) hashTable["StackTrace"]);

            if (result == "Failure")
            {
                var sb = new StringBuilder();
                foreach (var frame in stackTrace)
                {
                    sb.Append(frame);
                }

                var message = exception == null ? "Unknown exception" : exception.ToString();
                var stacktrace = sb.ToString();
                return new PowerShellTestResult(TestOutcome.Failed, message, stacktrace);
            }

            return new PowerShellTestResult(TestOutcome.Passed);
        }
    }
}
