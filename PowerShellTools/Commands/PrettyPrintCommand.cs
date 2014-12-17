using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PowerShellTools.Commands
{
    /// <summary>
    ///     This command is used to reformat the currently selected PowerShell script.
    /// </summary>
    /// <remarks>
    ///     This command appears in the right-click context menu of a PowerShell script.
    ///     This command executes the PrettyPrint.ps1 script inside this project.
    /// </remarks>
    public class PrettyPrintCommand : ICommand
    {
        public CommandID CommandId
        {
            get
            {
                return new CommandID(new Guid(GuidList.CmdSetGuid), (int)GuidList.CmdidPrettyPrint);
            }
        }

        public void QueryStatus(object sender, EventArgs args)
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null && dte2.ActiveDocument != null && dte2.ActiveDocument.Language == "PowerShell")
            {
                var menuItem = sender as OleMenuCommand;
                if (menuItem != null)
                {
                    menuItem.Visible = true;
                    menuItem.Supported = true;
                    menuItem.Enabled = true;
                }
            }
        }

        static public string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public void Execute(object sender, EventArgs args)
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null)
            {
                var path = dte2.ActiveDocument.FullName;

                var scriptContents = File.ReadAllText(path);
                string prettyContents;

                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = _runspace;

                    var script = Path.Combine(AssemblyDirectory, "PrettyPrint.ps1");
                    ps.Commands.AddScript("Import-Module '" + script + "'");
                    ps.Invoke();
                    
                    ps.Commands.Clear();
                    ps.Commands.AddScript("Format-Script -Path '" + path + "' -AsString");

                    prettyContents = ps.Invoke<string>().FirstOrDefault();
                }

                dte2.ActiveDocument.ReplaceText(scriptContents, prettyContents);
            }
        }

        private readonly Runspace _runspace;

        public PrettyPrintCommand(Runspace runspace)
        {
            _runspace = runspace;
        }
    }
}
