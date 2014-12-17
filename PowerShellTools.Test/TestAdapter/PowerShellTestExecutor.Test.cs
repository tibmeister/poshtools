using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PowerShellTools.DebugEngine.Impl;
using PowerShellTools.TestAdapter;

namespace PowerShellTools.Test.TestAdapter
{
    [TestClass]
    public class PowerShellTestExecutorTest
    {
        private PowerShellTestExecutor _test;

        private Mock<PowerShellTestExecutorBase> _executorMock;
        private Mock<IRunContext> _runContext;
        private Mock<IFrameworkHandle> _frameworkHandle;

        [TestInitialize]
        public void Init()
        {
            _executorMock = new Mock<PowerShellTestExecutorBase>();
            _runContext = new Mock<IRunContext>();
            _frameworkHandle = new Mock<IFrameworkHandle>();
            _test = new PowerShellTestExecutor(new List<PowerShellTestExecutorBase> { _executorMock.Object});
        }

        [TestMethod]
        public void ShouldHaveExtensionUriAttribute()
        {
            var attribute = typeof (PowerShellTestExecutor).GetCustomAttributes(typeof (ExtensionUriAttribute), false).FirstOrDefault() as
                ExtensionUriAttribute;

            Assert.IsNotNull(attribute);
        }

        [TestMethod]
        public void ExtensionUriAttributeShouldHavePowerShellUri()
        {
            var attribute = typeof(PowerShellTestExecutor).GetCustomAttributes(typeof(ExtensionUriAttribute), false).FirstOrDefault() as
                ExtensionUriAttribute;

            Assert.AreEqual(PowerShellTestExecutor.ExecutorUri, attribute.ExtensionUri);
        }

        [TestMethod]
        public void ShouldSendErrorIfNoTestExecutorFound()
        {
            var testCase = new TestCase("Blagh||Test", new Uri("http://executor"), "adsfdsdaf");

            _executorMock.Setup(m => m.TestFramework).Returns("NotBlagh");
            _test.RunTests(new []{testCase}, _runContext.Object, _frameworkHandle.Object);

            _frameworkHandle.Verify(m => m.SendMessage(TestMessageLevel.Error, "Unknown test executor: Blagh"));
        }


        [TestMethod]
        public void ShouldFailTestIfExceptionIsThrownFromExecutor()
        {
            var testCase = new TestCase("Blagh||Test", new Uri("http://executor"), "adsfdsdaf");

            _executorMock.Setup(m => m.TestFramework).Returns("Blagh");
            _executorMock.Setup(m => m.RunTest(It.IsAny<PowerShell>(), testCase, _runContext.Object))
                .Throws(new Exception("Error!!"));

            TestResult result = null;
            _frameworkHandle.Setup(m => m.RecordResult(It.IsAny<TestResult>())).Callback<TestResult>(x => result = x);

            _test.RunTests(new[] { testCase }, _runContext.Object, _frameworkHandle.Object);

            Assert.AreEqual(TestOutcome.Failed, result.Outcome);
            Assert.AreEqual("Error!!", result.ErrorMessage);
        }

        [TestMethod]
        public void ShouldReturnResultOfExecutorRunTest()
        {
            var testCase = new TestCase("Blagh||Test", new Uri("http://executor"), "adsfdsdaf");

            var poshResult = new PowerShellTestResult(TestOutcome.Failed, "Error!!", "Blagh!");

            _executorMock.Setup(m => m.TestFramework).Returns("Blagh");
            _executorMock.Setup(m => m.RunTest(It.IsAny<PowerShell>(), testCase, _runContext.Object))
                .Returns(poshResult);

            TestResult result = null;
            _frameworkHandle.Setup(m => m.RecordResult(It.IsAny<TestResult>())).Callback<TestResult>(x => result = x);

            _test.RunTests(new[] { testCase }, _runContext.Object, _frameworkHandle.Object);

            Assert.AreEqual(TestOutcome.Failed, result.Outcome);
            Assert.AreEqual("Error!!", result.ErrorMessage);
            Assert.AreEqual("Blagh!", result.ErrorStackTrace);
        }
    }
}
