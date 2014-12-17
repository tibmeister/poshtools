using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PowerShellTools.TestAdapter;

namespace PowerShellTools.Test.TestAdapter
{
    [TestClass]
    public class PowerShellTestDiscovererTest
    {
        private PowerShellTestDiscoverer _discoverer;
        private string _tempFile;
        private Mock<IDiscoveryContext> _discoveryContext;
        private Mock<ITestCaseDiscoverySink> _sink;
        private Mock<IMessageLogger> _messageLogger;

        [TestInitialize]
        public void Init()
        {
            _discoverer = new PowerShellTestDiscoverer();

            _discoveryContext = new Mock<IDiscoveryContext>();
            _sink = new Mock<ITestCaseDiscoverySink>();
            _messageLogger = new Mock<IMessageLogger>();
        }

        [TestCleanup]
        public void Clean()
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }

        private string WriteTestFile(string contents)
        {
            _tempFile = Path.GetTempFileName();
            File.WriteAllText(_tempFile, contents);
            return _tempFile;
        }

        [TestMethod]
        public void ShouldHaveFileExtensionAttributeOnClass()
        {
            var fileExtensionAttribute = typeof (PowerShellTestDiscoverer).GetCustomAttributes(typeof(FileExtensionAttribute), false).FirstOrDefault() as FileExtensionAttribute;
            Assert.IsNotNull(fileExtensionAttribute, "File extension attribute missing.");
            Assert.AreEqual(".ps1", fileExtensionAttribute.FileExtension);
        }

        [TestMethod]
        public void ShouldHaveDefaultExecutorUriAttributeOnClass()
        {
            var attribute = typeof(PowerShellTestDiscoverer).GetCustomAttributes(typeof(DefaultExecutorUriAttribute), false).FirstOrDefault() as DefaultExecutorUriAttribute;
            Assert.IsNotNull(attribute, "DefaultExecutorUriAttribute missing.");
            Assert.AreEqual(PowerShellTestExecutor.ExecutorUri, attribute.ExecutorUri);
        }

        [TestMethod]
        public void ShouldDiscoverPesterTest()
        {
            const string testScript = @"
            Describe 'BuildIfChanged' {
                Context 'When there are changes' {
                    It 'Builds next version' {

                    }
                }
            }";

            var tempFile = WriteTestFile(testScript);

            var testCases = new List<TestCase>();
            _sink.Setup(m => m.SendTestCase(It.IsAny<TestCase>())).Callback<TestCase>(testCases.Add);

            _discoverer.DiscoverTests(new []{tempFile}, _discoveryContext.Object, _messageLogger.Object, _sink.Object);

            Assert.IsTrue(testCases.Any(), "No test cases found.");
            Assert.AreEqual("When there are changes It Builds next version", testCases[0].DisplayName);
            Assert.AreEqual(PowerShellTestExecutor.ExecutorUri, testCases[0].ExecutorUri);
            Assert.AreEqual(tempFile, testCases[0].CodeFilePath);
            Assert.AreEqual(4, testCases[0].LineNumber);
        }

        [TestMethod]
        public void ShouldSupportPesterNameParameterOnDescribeBlock()
        {
            const string testScript = @"
            Describe -Name 'BuildIfChanged' {
                Context 'ThisIsATest' {
                    It -Name 'Something' {
                    }
                }
            }";

            var tempFile = WriteTestFile(testScript);

            var testCases = new List<TestCase>();
            _sink.Setup(m => m.SendTestCase(It.IsAny<TestCase>())).Callback<TestCase>(testCases.Add);

            _discoverer.DiscoverTests(new[] { tempFile }, _discoveryContext.Object, _messageLogger.Object, _sink.Object);

            Assert.IsTrue(testCases.Any(), "No test cases found.");
            Assert.AreEqual("ThisIsATest It Something", testCases[0].DisplayName);
        }

        [TestMethod]
        public void ShouldReturnCorrectFdqn()
        {
            const string testScript = @"
            Describe -Name 'BuildIfChanged' {
                Context 'ThisIsATest' {
                    It -Name 'Something' {
                    }
                }
            }";

            var tempFile = WriteTestFile(testScript);

            var testCases = new List<TestCase>();
            _sink.Setup(m => m.SendTestCase(It.IsAny<TestCase>())).Callback<TestCase>(testCases.Add);

            _discoverer.DiscoverTests(new[] { tempFile }, _discoveryContext.Object, _messageLogger.Object, _sink.Object);

            Assert.IsTrue(testCases.Any(), "No test cases found.");
            Assert.AreEqual("Pester||BuildIfChanged||ThisIsATest||Something", testCases[0].FullyQualifiedName);
        }

        [TestMethod]
        public void ShouldDiscoverPSateTestCases()
        {
            const string testScript = @"#psate
            TestFixture 'BuildIfChanged' {
                TestCase 'ThisIsATest' {
                }
            }";

            var tempFile = WriteTestFile(testScript);

            var testCases = new List<TestCase>();
            _sink.Setup(m => m.SendTestCase(It.IsAny<TestCase>())).Callback<TestCase>(testCases.Add);

            _discoverer.DiscoverTests(new[] { tempFile }, _discoveryContext.Object, _messageLogger.Object, _sink.Object);

            Assert.IsTrue(testCases.Any(), "No test cases found.");
            Assert.AreEqual("ThisIsATest", testCases[0].DisplayName);
            Assert.AreEqual(PowerShellTestExecutor.ExecutorUri, testCases[0].ExecutorUri);
            Assert.AreEqual(tempFile, testCases[0].CodeFilePath);
            Assert.AreEqual(3, testCases[0].LineNumber);
            Assert.AreEqual("PSate||BuildIfChanged||ThisIsATest", testCases[0].FullyQualifiedName);
        }

        [TestMethod]
        public void ShouldSupportNameOnTestFixtureAndTestCase()
        {
            const string testScript = @"#psate
            TestFixture -Name 'BuildIfChanged' {
                TestCase -Name 'ThisIsATest' {
                }
            }";

            var tempFile = WriteTestFile(testScript);

            var testCases = new List<TestCase>();
            _sink.Setup(m => m.SendTestCase(It.IsAny<TestCase>())).Callback<TestCase>(testCases.Add);

            _discoverer.DiscoverTests(new[] { tempFile }, _discoveryContext.Object, _messageLogger.Object, _sink.Object);

            Assert.IsTrue(testCases.Any(), "No test cases found.");
            Assert.AreEqual("ThisIsATest", testCases[0].DisplayName);
            Assert.AreEqual("PSate||BuildIfChanged||ThisIsATest", testCases[0].FullyQualifiedName);
        }
    }
}
