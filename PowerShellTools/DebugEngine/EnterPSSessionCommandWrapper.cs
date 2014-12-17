using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;

namespace PowerShellTools.DebugEngine
{
    public static class EnterPSSessionCommandWrapper
    {
        // RemotePipeline ConnectRunningPipeline(RemoteRunspace remoteRunspace)
        public static Pipeline ConnectRunningPipeline(Runspace remoteRunspace)
        {
            var remoteRunspaceType = typeof (EnterPSSessionCommand).Assembly.GetType("System.Management.Automation.RemoteRunspace");
            var method = typeof (EnterPSSessionCommand).GetMethod("ConnectRunningPipeline",
                BindingFlags.NonPublic | BindingFlags.Static, null, new [] {remoteRunspaceType}, null);

            var remotePipeline = method.Invoke(null, new[] {remoteRunspace});

            return remotePipeline as Pipeline;
        }

        // void ContinueCommand(RemoteRunspace remoteRunspace, Pipeline cmd, PSHost host, bool inDebugMode, ExecutionContext context)
        public static void ContinueCommand(Runspace remoteRunspace, Pipeline cmd, PSHost host, bool inDebugMode,
            Runspace oldRunspace)
        {
            var remoteRunspaceType = typeof(EnterPSSessionCommand).Assembly.GetType("System.Management.Automation.RemoteRunspace");
            var executionContextType = typeof(EnterPSSessionCommand).Assembly.GetType("System.Management.Automation.ExecutionContext");
            var executionContext = typeof (Runspace).GetProperty("ExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true).Invoke(remoteRunspace, new object[]{});
            var method = typeof(EnterPSSessionCommand).GetMethod("ContinueCommand",
                BindingFlags.NonPublic | BindingFlags.Static, null, new[] { remoteRunspaceType, typeof(Pipeline), typeof(PSHost), typeof(bool), executionContextType }, null);

            method.Invoke(null, new[] { remoteRunspace, cmd, host, inDebugMode, executionContext});
        }
    }
}
