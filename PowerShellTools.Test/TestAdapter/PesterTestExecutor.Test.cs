using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PowerShellTools.TestAdapter;

namespace PowerShellTools.Test.TestAdapter
{
    [TestClass]
    [DeploymentItem(@"Pester\", @"packages\Pester\tools")]
    public class PesterTestExecutorTest
    {
        private PesterTestExecutor _executor;
        private string _tempFile;
        private string _pesterTestDir;
        private Mock<IRunContext> _runContext;
        private Runspace _runspace;
        private PowerShell _powerShell;

        public TestContext TestContext { get; set; }


        [TestInitialize]
        public void Init()
        {
            _executor = new PesterTestExecutor();

            _runContext = new Mock<IRunContext>();

            _runspace = RunspaceFactory.CreateRunspace(new TestAdapterHost());
            _runspace.Open();
            _powerShell = PowerShell.Create();
            _powerShell.Runspace = _runspace;
        }

        [TestCleanup]
        public void Clean()
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }

            if (Directory.Exists(_pesterTestDir))
            {
                Directory.Delete(_pesterTestDir);
            }

            if (_runspace != null)
            {
                _runspace.Dispose();
            }
        }

        private TestCase WriteTestFile(string name, string contents)
        {
            _pesterTestDir = Path.Combine(Path.GetTempPath(), DateTime.Now.Ticks.ToString());

            Directory.CreateDirectory(_pesterTestDir);

            _tempFile = Path.Combine(_pesterTestDir, "MyTests.Tests.ps1");
            File.WriteAllText(_tempFile, contents);

            var testCase = new TestCase(name, new Uri("http://test.com"), _tempFile);
            testCase.CodeFilePath = _tempFile;
            return testCase;
        }

        [TestMethod]
        public void ShouldReturnSuccessfulTestResults()
        {
            const string testScript = @"
            Describe 'Test' {
                Context 'Blah' {
                     It 'Should pass' {
                         1 | Should be 1
                     }
                }
            }
            ";

            _runContext.Setup(m => m.TestRunDirectory).Returns(TestContext.TestDeploymentDir);
            _runContext.Setup(m => m.SolutionDirectory).Returns(TestContext.TestDeploymentDir);

            var testCase = WriteTestFile("Pester||Test||Blah||Should pass", testScript); 
            var result = _executor.RunTest(_powerShell, testCase, _runContext.Object);

            Assert.AreEqual(TestOutcome.Passed, result.Outcome);
        }

        [TestMethod]
        public void ShouldReturnUnsuccessfulTestResult()
        {
            const string testScript = @"
            Describe 'Test' {
                Context 'Blah' {
                    It 'Should fail' {
                        1 | Should Be 2
                    }
                }
            }
            ";

            _runContext.Setup(m => m.TestRunDirectory).Returns(TestContext.TestDeploymentDir);
            _runContext.Setup(m => m.SolutionDirectory).Returns(TestContext.TestDeploymentDir);

            var testFile = WriteTestFile("Pester||Test||Blah||Should fail",testScript);

            var result = _executor.RunTest(_powerShell, testFile, _runContext.Object);

            Assert.AreEqual(TestOutcome.Failed, result.Outcome);
            Assert.IsTrue(result.ErrorMessage.StartsWith("Failure [Should fail]"));
            Assert.AreEqual("at line: 5 in " + testFile.CodeFilePath, result.ErrorStacktrace);
        }

        [TestMethod]
        public void ShouldReturnUnsuccessfulTestResultForAnException()
        {
            const string testScript = @"
            Describe 'Test' {
                Context 'Blah' {
                    It 'Should fail' {
                        throw 'This sucks!'
                    }
                }
            }
            ";

            _runContext.Setup(m => m.TestRunDirectory).Returns(TestContext.TestDeploymentDir);
            _runContext.Setup(m => m.SolutionDirectory).Returns(TestContext.TestDeploymentDir);

            var testFile = WriteTestFile("Pester||Test||Blah||Should fail", testScript);

            var result = _executor.RunTest(_powerShell, testFile, _runContext.Object);

            Assert.AreEqual(TestOutcome.Failed, result.Outcome);
            Assert.AreEqual("Failure [Should fail]\r\nThis sucks!\r\n", result.ErrorMessage);
            Assert.AreEqual("at line: 5 in " + testFile.CodeFilePath, result.ErrorStacktrace);
        }
    }
}

