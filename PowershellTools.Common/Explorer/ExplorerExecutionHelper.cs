using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.Common
{
    internal static class ExplorerExecutionHelper
    {
        internal static PSDataCollection<T> ExecuteCommand<T>(string command)
        {
            PSDataCollection<T> outputCollection = new PSDataCollection<T>();

            using (PowerShell powerShell = PowerShell.Create())
            {
                powerShell.AddScript(command);
                powerShell.Invoke<T>(null, outputCollection);
            }

            return outputCollection;
        }
    }
}
