using System;
using System.Management.Automation;
using Microsoft.VisualStudio.Shell;

namespace PowerShellTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "VSService")]
    public class GetVisualStudioServiceCommand : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public Type InterfaceType { get; set; }

        [Parameter]
        public SwitchParameter Global { get; set; }

        protected override void BeginProcessing()
        {
            object service = null;
            if (Global)
            {
                service = Package.GetGlobalService(InterfaceType);
            }
            else
            {
                service = PowerShellToolsPackage.Instance.GetService(InterfaceType);
            }

            if (service == null)
            {
                throw new ArgumentException(String.Format("Unknow service type [{0}]", InterfaceType));
            }

            WriteObject(service);
        }
    }
}
