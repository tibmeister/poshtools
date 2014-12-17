using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerShellTools.DebugEngine;

namespace PowerShellTools.Test
{
    [TestClass]
    public class VsxHostTest
    {
        private ScriptDebugger _host;

        [TestInitialize]
        public void Init()
        {
            _host = new ScriptDebugger(true, null);
        }

        [TestMethod]
        public void TestWriteHost()
        {
            var command = new Command("Write-Host");
            command.Parameters.Add("Object", "Test");

            string output = "";
            _host.HostUi.OutputString = x =>
            {
                output += x;
            };

            using (var pipe = _host.Runspace.CreatePipeline())
            {
                pipe.Commands.Add(command);
                pipe.Invoke();
            }

            Assert.AreEqual("Test\n", output);
        }
    }
}
