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
    [DeploymentItem(@"PSate\", @"packages\PSate\tools")]
    public class PSateTestExecutorTest
    {
        private PsateTestExecutor _executor;
        private string _tempFile;
        private string _pesterTestDir;
        private Mock<IRunContext> _runContext;
        private Runspace _runspace;
        private PowerShell _powerShell;

        public TestContext TestContext { get; set; }


        [TestInitialize]
        public void Init()
        {
            _executor = new PsateTestExecutor();

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

        private TestCase WriteTestFile(string testName, string contents)
        {
            _pesterTestDir = Path.Combine(Path.GetTempPath(), DateTime.Now.Ticks.ToString());

            Directory.CreateDirectory(_pesterTestDir);

            _tempFile = Path.Combine(_pesterTestDir, "MyTests.Tests.ps1");
            File.WriteAllText(_tempFile, contents);

            var testCase = new TestCase(testName, new Uri("http://something.com"), _tempFile);
            testCase.CodeFilePath = _tempFile;
            return testCase;
        }

        [TestMethod]
        public void ShouldReturnSuccessfulTestResults()
        {
            const string testScript = @"#psate
            TestFixture 'Test' {
                TestCase 'Blah' {
                }
            }
            ";

            _runContext.Setup(m => m.TestRunDirectory).Returns(TestContext.TestDeploymentDir);
            _runContext.Setup(m => m.SolutionDirectory).Returns(TestContext.TestDeploymentDir);

            var testFile = WriteTestFile("PSate||Test||Blah", testScript);

            var result = _executor.RunTest(_powerShell, testFile, _runContext.Object);

            Assert.AreEqual(TestOutcome.Passed, result.Outcome);
        }

        [TestMethod]
        public void ShouldReturnUnsuccessfulTestResultIfExceptionThrown()
        {
            const string testScript = @"#psate
            TestFixture 'Test' {
                TestCase 'Blah' {
                    throw 'Boom!'
                }
            }
            ";

            _runContext.Setup(m => m.TestRunDirectory).Returns(TestContext.TestDeploymentDir);
            _runContext.Setup(m => m.SolutionDirectory).Returns(TestContext.TestDeploymentDir);

            var testFile = WriteTestFile("PSate||Test||Blah", testScript);

            var result = _executor.RunTest(_powerShell, testFile, _runContext.Object);

            Assert.AreEqual("Boom!", result.ErrorMessage);
            Assert.AreEqual(TestOutcome.Failed, result.Outcome);
        }
    }
}
