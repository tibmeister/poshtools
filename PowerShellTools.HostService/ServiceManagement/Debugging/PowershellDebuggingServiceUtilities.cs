using EnvDTE80;
using Microsoft.PowerShell;
using PowerShellTools.Common.Debugging;
using PowerShellTools.Common.IntelliSense;
using PowerShellTools.Common.ServiceManagement.DebuggingContract;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PowerShellTools.HostService.ServiceManagement.Debugging
{
    /// <summary>
    /// Utility functions for powershell debugging service
    /// </summary>
    public partial class PowerShellDebuggingService
    {
        // Potential TODO: Refactor this class into either a static Utilities class

        private const string DteVariableName = "dte";

        private void SetRunspace(Runspace runspace)
        {
            if (_runspace != null)
            {
                UnloadRunspace(_runspace);
            }

            _runspace = runspace;
            LoadRunspace(_runspace);

            ProvideDteVariable(_runspace);
        }

        private void LoadRunspace(Runspace runspace)
        {
            if (_runspace != null)
            {
                if (_runspace.ConnectionInfo == null || _installedPowerShellVersion >= RequiredPowerShellVersionForRemoteSessionDebugging)
                {
                    runspace.Debugger.DebuggerStop += Debugger_DebuggerStop;
                    runspace.Debugger.BreakpointUpdated += Debugger_BreakpointUpdated;
                }

                runspace.StateChanged += _runspace_StateChanged;
            }
        }

        private void UnloadRunspace(Runspace runspace)
        {
            if (_runspace != null)
            {
                if (_runspace.ConnectionInfo == null || _installedPowerShellVersion >= RequiredPowerShellVersionForRemoteSessionDebugging)
                {
                    runspace.Debugger.DebuggerStop -= Debugger_DebuggerStop;
                    runspace.Debugger.BreakpointUpdated -= Debugger_BreakpointUpdated;
                }
            }

            runspace.StateChanged -= _runspace_StateChanged;
        }

        private void RefreshScopedVariable()
        {
            ServiceCommon.Log("Debuggger stopped, let us retreive all local variable in scope");
            using (var pipeline = (_runspace.CreateNestedPipeline()))
            {
                var command = new Command("Get-Variable");
                pipeline.Commands.Add(command);
                _varaiables = pipeline.Invoke();
            }
        }

        private void RefreshCallStack()
        {
            ServiceCommon.Log("Debuggger stopped, let us retreive all call stack frames");
            using (var pipeline = (_runspace.CreateNestedPipeline()))
            {
                var command = new Command("Get-PSCallstack");
                pipeline.Commands.Add(command);
                _callstack = pipeline.Invoke();
            }
        }

        private void OnTerminatingException(Exception ex)
        {
            ServiceCommon.Log("OnTerminatingException");
            UnloadRunspace(_runspace);
            if (_callback != null)
            {
                _callback.TerminatingException(new PowerShellRunTerminatingException(ex));
            }
        }

        private void DebuggerFinished()
        {
            ServiceCommon.Log("DebuggerFinished");

            ClearBreakpoints();
            _psBreakpointTable.Clear();

            if (_callback != null)
            {
                _callback.OutputStringLine(string.Empty);
                _callback.RefreshPrompt();
            }

            if (_runspace != null)
            {
                UnloadRunspace(_runspace);
            }

            if (_currentPowerShell != null)
            {
                _currentPowerShell.Stop();
                _currentPowerShell = null;
            }

            ReleaseWaitHandler();

            _debuggingCommand = string.Empty;
            _localVariables.Clear();
            _propVariables.Clear();

            if (_callback != null)
            {
                _callback.DebuggerFinished();
            }
        }

        private void InitializeRunspace(PSHost psHost)
        {
            ServiceCommon.Log("Initializing run space with debugger");
            InitialSessionState iss = InitialSessionState.CreateDefault();
            iss.ApartmentState = ApartmentState.STA;
            iss.ThreadOptions = PSThreadOptions.ReuseThread;

            _runspace = RunspaceFactory.CreateRunspace(psHost, iss);
            _runspace.Open();

            ImportPoshToolsModule();
            LoadProfile();
            ServiceCommon.Log("Done initializing runspace");
        }

        private void ImportPoshToolsModule()
        {
            ServiceCommon.Log("Importing posh tools module");
            using (PowerShell ps = PowerShell.Create())
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                ps.Runspace = _runspace;
                ps.AddScript("Import-Module '" + assemblyLocation + "'");
                ps.Invoke();
            }
        }

        private void LoadProfile()
        {
            ServiceCommon.Log("Loading PowerShell Profile");
            using (PowerShell ps = PowerShell.Create())
            {
                var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var windowsPowerShell = Path.Combine(myDocuments, "WindowsPowerShell");
                var profile = Path.Combine(windowsPowerShell, "PoshTools_profile.ps1");

                var fi = new FileInfo(profile);
                if (!fi.Exists)
                {
                    return;
                }

                ps.Runspace = _runspace;
                ps.AddScript(". '" + profile + "'");
                ps.Invoke();
            }
        }

        private void SetupExecutionPolicy()
        {
            SetExecutionPolicy(ExecutionPolicy.RemoteSigned, ExecutionPolicyScope.Process);
        }

        private void SetExecutionPolicy(ExecutionPolicy policy, ExecutionPolicyScope scope)
        {
            ServiceCommon.Log("Setting execution policy");
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = _runspace;
                ps.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("ExecutionPolicy", policy)
                    .AddParameter("Scope", scope)
                    .AddParameter("Force");
                ps.Invoke();
            }
        }

        private object RetrieveVariable(string varName)
        {
            var psVar = _localVariables.FirstOrDefault(v => v.Name == varName);
            object psVariable = (psVar == null) ? null : psVar.Value;

            if (psVariable == null && _propVariables.ContainsKey(varName))
            {
                psVariable = _propVariables[varName];
            }

            return psVariable;
        }

        private void ReleaseWaitHandler()
        {
            _resumeAction = DebugEngineConstants.Debugger_Stop;
            _pausedEvent.Set();
        }

        /// <summary>
        /// Provides the $dte Variable if we are in a local runspace and if the $dte variable has not yet been set.
        /// </summary>
        private static void ProvideDteVariable(Runspace runspace)
        {
            // only do this when we are working with a local runspace
            if (runspace.ConnectionInfo == null)
            {
                // Preset dte as PS variable if not yet
                if (runspace.SessionStateProxy.PSVariable.Get(DteVariableName) == null)
                {
                    ServiceCommon.Log("Providing $dte variable to the local runspace.");

                    DTE2 dte = DTEManager.GetDTE(Program.VsProcessId);

                    if (dte != null)
                    {
                        // We want to make $dte constant so that it can't be overridden; similar to the $psISE analog

                        PSVariable dteVar = new PSVariable(DteVariableName, dte, ScopedItemOptions.Constant);

                        runspace.SessionStateProxy.PSVariable.Set(dteVar);
                    }
                    else
                    {
                        ServiceCommon.Log("Dte object not found.");
                    }
                }
            }
        }

        private string ExecuteDebuggingCommand(string debuggingCommand, bool output)
        {
            // Need to be thread-safe here, to ensure every debugging command get processed.
            // e.g: Set/Enable/Disable/Remove breakpoint during debugging 
            lock (_executeDebugCommandLock)
            {
                ServiceCommon.Log("Client asks for executing debugging command");
                _debugOutput = output;
                _debugCommandOutput = string.Empty;
                _debuggingCommand = debuggingCommand;
                _debugCommandEvent.Reset();
                _pausedEvent.Set();
                _debugCommandEvent.WaitOne();
                _debugOutput = true;
                _debuggingCommand = string.Empty;
                return _debugCommandOutput;
            }
        }

        private void objects_DataAdded(object sender, DataAddedEventArgs e)
        {
            var list = sender as PSDataCollection<PSObject>;
            StringBuilder outputString = new StringBuilder();
            foreach (PSObject obj in list)
            {
                outputString.AppendLine(obj.ToString());
            }

            if (_debugOutput)
            {
                NotifyOutputString(outputString.ToString());
            }
        }
    }
}
