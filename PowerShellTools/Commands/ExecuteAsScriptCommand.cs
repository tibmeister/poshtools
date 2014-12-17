using System;
using System.ComponentModel.Design;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerShellTools.Project;

namespace PowerShellTools.Commands
{
    /// <summary>
    ///     Command for executing a script.
    /// </summary>
    /// <remarks>
    ///     This command appears in the right-click context menu inside a PowerShell script.
    /// </remarks>
    internal class ExecuteAsScriptCommand : ICommand
    {
        public CommandID CommandId
        {
            get
            {
                return new CommandID(new Guid(GuidList.CmdSetGuid), (int)GuidList.CmdidExecuteAsScript);
            }
        }
        public void Execute(object sender, EventArgs args)
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null && dte2.ActiveDocument != null)
            {
                var launcher = new PowerShellProjectLauncher();
                launcher.LaunchFile(dte2.ActiveDocument.FullName, true);
            }
        }

        public void QueryStatus(object sender, EventArgs args)
        {
            bool bVisible = false;

            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null && dte2.ActiveDocument != null && dte2.ActiveDocument.Language == "PowerShell")
            {
                bVisible = true;
            }

            var menuItem = sender as OleMenuCommand;
            if (menuItem != null)
            {
                menuItem.Visible = bVisible;
            }
        }
    }
}
