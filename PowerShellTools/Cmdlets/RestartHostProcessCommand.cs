using System.Diagnostics;
using System.Management.Automation;
using System.Windows.Forms;
using PowerShellTools.ServiceManagement;

namespace PowerShellTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Restart, "HostProcess")]
    public class RestartHostProcessCommand : Cmdlet
    {
        protected override void BeginProcessing()
        {
            Application.Exit();
        }
    }
}
