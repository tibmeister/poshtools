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

        public const string ExplorerHostRelativeUri = "NamedPipePowerShellExplorer";

        public const string ReadyEventPrefix = "VsPowershellToolProcess:";

        // 20 minutes
        public const int HostProcessStartupTimeout = 20 * 60 * 1000; // wait for 20 minutes max for remote powershell host process to initialize

        public const string PowerShellHostExeName = "PowerShellToolsProcessHost.exe";

        public const string PowerShellHostExeNameForx86 = "PowerShellToolsProcessHost.x86.exe";

        public const string PowerShellHostConsoleExeName = "PowerShellToolsProcessHostConsole.exe";

        // 10M in bytes
        public const int BindingMaxReceivedMessageSize = 10000000;

        // Arguments for vspowershellhost.exe
        public const string VsProcessIdArg = "/vsPid:";
        public const string UniqueEndpointArg = "/endpoint:";
        public const string ReadyEventUniqueNameArg = "/readyEventUniqueName:";

        // Arguments for vspowershellhostconsole.exe
        public const string ConsoleProcessIdArg = "/hpPid:";

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

        /// <summary>
        /// This is the GUID in string form of the Visual Studio UI Context when in PowerShell REPL window is opened.
        /// </summary>
        public const string PowerShellReplCreationUiContextString = "310d9a74-0a72-4b83-8c5b-4e75f035214c";

        /// <summary>
        /// This is the GUID of the Visual Studio UI Context when in PowerShell REPL window is opened.
        /// </summary>
        public static readonly Guid PowerShellReplCreationUiContextGuid = new Guid(PowerShellReplCreationUiContextString);

        /// <summary>
        /// This is the GUID in string form of the Visual Studio UI Context when in PowerShell project is opened/created.
        /// </summary>
        public const string PowerShellProjectUiContextString = "8b1141ab-519d-4c1e-a86c-510e5a56bf64";

        /// <summary>
        /// This is the GUID of the Visual Studio UI Context when in PowerShell project is opened/created.
        /// </summary>
        public static readonly Guid PowerShellProjectUiContextGuid = new Guid(PowerShellProjectUiContextString);

        /// <summary>
        /// Minimal width of REPL output buffer as 80 to keep consistent experience with another PowerShell custom host in VS: Nuget Manager Console
        /// </summary>
        public const int MinimalReplBufferWidth = 80;

        /// <summary>
        /// This is the GUID in string form for the PowerShell Tools remote debugging port supplier.
        /// </summary>
        public const string PortSupplierId = "{c9b338d2-7e0b-42a3-af51-7e33a7e349ba}";

        /// <summary>
        /// This is the GUID for the PowerShell Tools remote debugging port supplier.
        /// </summary>
        public static readonly Guid PortSupplierGuid = new Guid(PortSupplierId);

        /// <summary>
        /// This is the GUID in string form for the PowerShell Tools unsecured remote debugging port supplier.
        /// </summary>
        public const string UnsecuredPortSupplierId = "{44379988-7b12-4cfe-b02a-d56f751d597a}";

        /// <summary>
        /// This is the GUID for the PowerShell Tools unsecured remote debugging port supplier.
        /// </summary>
        public static readonly Guid UnsecuredPortSupplierGuid = new Guid(UnsecuredPortSupplierId);
    }
}
