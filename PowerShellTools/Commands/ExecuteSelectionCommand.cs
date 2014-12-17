using System;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerShellTools.Project;

namespace PowerShellTools.Commands
{
    /// <summary>
    ///     This command executes the currently selected code in a PowerShell script.
    /// </summary>
    /// <remarks>
    ///     This command appears in the right-click context menu of a PowerShell script.
    /// </remarks>
    internal class ExecuteSelectionCommand : ICommand
    {
        public CommandID CommandId
        {
            get
            {
                return new CommandID(new Guid(GuidList.CmdSetGuid), (int)GuidList.CmdidExecuteSelection);
            }
        }

        public void Execute(object sender, EventArgs args)
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null && dte2.ActiveDocument != null)
            {
                var launcher = new PowerShellProjectLauncher();

                TextSelection sel = (TextSelection)dte2.ActiveDocument.Selection;
                if (sel.TopPoint.EqualTo(sel.ActivePoint))
                {
                    sel.SelectLine();

                    launcher.LaunchSelection(sel.Text);
                }
                else
                {
                    launcher.LaunchSelection(sel.Text);
                }
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
                menuItem.Supported = bVisible;
                menuItem.Enabled = bVisible;
            }
        }
    }
}
