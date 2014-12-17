using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PowerShellTools.DebugEngine;

namespace PowerShellTools.Test
{
    [TestClass]
    [DeploymentItem("TestFile.ps1")]
    public class ScriptDebuggerTest
    {
        private ScriptDebugger _debugger;
        private Runspace _runspace; 

        [TestInitialize]
        public void Init()
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
        }

        [TestCleanup]
        public void Clean()
        {
            _runspace.Dispose();
            _runspace = null;
        }

        [TestMethod]
        public void ShouldClearBreakpoints()
        {
            using (var pipe = _runspace.CreatePipeline())
            {
                var command = new Command("Set-PSBreakpoint");
                command.Parameters.Add("Script", ".\\TestFile.ps1");
                command.Parameters.Add("Line", 1);

                pipe.Commands.Add(command);
                pipe.Invoke();
            }

            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(new List<ScriptBreakpoint>());

            using (var pipe = _runspace.CreatePipeline())
            {
                pipe.Commands.Add("Get-PSBreakpoint");
                var breakpoints = pipe.Invoke();
                
                Assert.AreEqual(0, breakpoints.Count);
            }
        }

        [TestMethod]
        public void ShouldNotDieIfNoBreakpoints()
        {
            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);

            using (var pipe = _runspace.CreatePipeline())
            {
                pipe.Commands.Add("Get-PSBreakpoint");
                var breakpoints = pipe.Invoke();

                Assert.AreEqual(0, breakpoints.Count);
            }
        }


        [TestMethod]
        public void ShouldSetLineBreakpoint()
        {
            var engineEvents = new Mock<IEngineEvents>();

            var sbps = new List<ScriptBreakpoint>
                           {
                               new ScriptBreakpoint(null, ".\\TestFile.ps1", 1, 0, engineEvents.Object)
                           };

            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(sbps);

            using (var pipe = _runspace.CreatePipeline())
            {
                pipe.Commands.Add("Get-PSBreakpoint");
                var breakpoints = pipe.Invoke();

                //Verify the breakpoint was added to the runspace.
                Assert.AreEqual(1, breakpoints.Count);
            }

            //Verify the callback event was triggered.
            engineEvents.Verify(m => m.Breakpoint(null, sbps[0]), Times.Once());
        }

        [TestMethod]
        public void ShouldBreakOnBreakPoint()
        {
            var engineEvents = new Mock<IEngineEvents>();

            var fi = new FileInfo(".\\TestFile.ps1");

            var sbps = new List<ScriptBreakpoint>
                           {
                               new ScriptBreakpoint(null, fi.FullName, 3, 0, engineEvents.Object)
                           };

            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(sbps);

            var node = new ScriptProgramNode(null);
            node.IsFile = true;
            node.FileName = fi.FullName;

            var mre = new ManualResetEvent(false);

            bool breakpointHit = false;
            _debugger.BreakpointHit += (sender, args) => { breakpointHit = true; _debugger.Continue(); };
            _debugger.DebuggingFinished += (sender, args) => mre.Set();
            _debugger.Execute(node);

            Assert.IsTrue(mre.WaitOne(5000));
            Assert.IsTrue(breakpointHit);
        }

        [TestMethod]
        public void ShouldAcceptArguments()
        {
            var fi = new FileInfo(".\\TestFile.ps1");

            var sbps = new List<ScriptBreakpoint>();
            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(sbps);

            var node = new ScriptProgramNode(null);
            node.FileName = fi.FullName;
            node.IsFile = true;
            node.Arguments = "'Arg1' 'Arg2'";

            var mre = new ManualResetEvent(false);
            _debugger.DebuggingFinished += (sender, args) => mre.Set();

            _debugger.Execute(node);

            Assert.IsTrue(mre.WaitOne(5000));

            var arg1 = _runspace.SessionStateProxy.GetVariable("Argument1");
            var arg2 = _runspace.SessionStateProxy.GetVariable("Argument2");

            Assert.AreEqual("Arg1", arg1);
            Assert.AreEqual("Arg2", arg2);
        }

        [TestMethod]
        public void ShouldExecuteSnippet()
        {
            var sbps = new List<ScriptBreakpoint>();
            _debugger = new ScriptDebugger(true, null);
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(sbps);

            var node = new ScriptProgramNode(null);
            node.FileName = "$Global:MyVariable = 'Test'";

            var mre = new ManualResetEvent(false);
            _debugger.DebuggingFinished += (sender, args) => mre.Set();

            _debugger.Execute(node);

            Assert.IsTrue(mre.WaitOne(5000));

            var myVariable = _runspace.SessionStateProxy.GetVariable("MyVariable");

            Assert.AreEqual("Test", myVariable);
        }

        [TestMethod]
        [Ignore]
        public void ShouldSupportRemoteSession()
        {
            var sbps = new List<ScriptBreakpoint>();
            _debugger = new ScriptDebugger(true, null);

            _runspace.Dispose();
            _runspace = RunspaceFactory.CreateRunspace(_debugger);
            _runspace.Open();
            _debugger.SetRunspace(_runspace);
            _debugger.SetBreakpoints(sbps);

            var node = new ScriptProgramNode(null);
            node.FileName = "Enter-PSSession localhost";

            var mre = new ManualResetEvent(false);
            string outputString = null;
            _debugger.DebuggingFinished += (sender, args) => mre.Set();
            _debugger.OutputString += (sender, args) => outputString = args.Value;
            _debugger.Execute(node);

            Assert.IsTrue(mre.WaitOne(5000));

            mre.Reset();
            node = new ScriptProgramNode(null);
            node.FileName = "$host.Name";
            _debugger.Execute(node);
            Assert.IsTrue(mre.WaitOne(5000));

            Assert.AreEqual("ServerRemoteHost", outputString);
        }
    }
}
