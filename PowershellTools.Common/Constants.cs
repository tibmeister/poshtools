using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.Common
{
    internal static class Constants
    {
        public const string ProcessManagerHostUri = "net.pipe://localhost/";

        public const string IntelliSenseHostRelativeUri = "NamedPipePowershellIntelliSense";

        public const string DebuggingHostRelativeUri = "NamedPipePowershellDebugging";

        public const string ReadyEventPrefix = "VsPowershellToolProcess:";

        // 20 minutes
        public const int HostProcessStartupTimeout = 20 * 60 * 1000; // wait for 20 minutes max for remote powershell host process to initialize

        public const string PowershellHostExeName = "PowerShellToolsProcessHost.exe";

        // 10M in bytes
        public const int BindingMaxReceivedMessageSize = 10000000;

        // Arguments for vspowershellhost.exe
        public const string VsProcessIdArg = "/vsPid:";
        public const string UniqueEndpointArg = "/endpoint:";
        public const string ReadyEventUniqueNameArg = "/readyEventUniqueName:";

        public const string SecureStringFullTypeName = "system.security.securestring";
        public const string PSCredentialFullTypeName = "system.management.automation.pscredential";

        /// <summary>
        /// This is the GUID in string form of the Visual Studio UI Context when in PowerShell debug mode.
        /// </summary>
        public const string PowerShellDebuggingUiContextString = "A185A958-AD74-44E5-B343-1B6682DAB132";

        /// <summary>
        /// This is the GUID of the Visual Studio UI Context when in PowerShell debug mode.
        /// </summary>
        public static readonly Guid PowerShellDebuggingUiContextGuid = new Guid(PowerShellDebuggingUiContextString);
        
        /// <summary>
        /// PowerShell install version registry key
        /// </summary>
        public const string NewInstalledPowerShellRegKey = @"Software\Microsoft\PowerShell\3\PowerShellEngine";
        public const string LegacyInstalledPowershellRegKey = @"Software\Microsoft\PowerShell\1\PowerShellEngine";

        /// <summary>
        /// PowerShell install version registry key name
        /// </summary>
        public const string PowerShellVersionRegKeyName = "PowerShellVersion";

        /// <summary>
        /// Latest PowerShell install link
        /// </summary>
        public const string PowerShellInstallFWLink = "http://go.microsoft.com/fwlink/?LinkID=524571";
    }
}
