using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.Common.Debugging
{
    public static class DebugEngineConstants
    {
        /// <summary>
        /// Command format of executing powershell script file 
        /// </summary>
        /// <remarks>
        /// {0} - command
        /// {1} - arguements
        /// </remarks>
        public const string ExecutionCommandFormat = ". '{0}' {1}";


        /// <summary>
        /// Match if that is an execution command
        /// </summary>
        /// <remarks>
        /// Pattern sample: Maching pattern like ". 'c:\test\test.ps1' -param1 val"
        /// </remarks>
        public const string ExecutionCommandPattern = @"^\.\s\'.*?\'.*$";

        /// <summary>
        /// Match the script file name from execution command
        /// </summary>
        /// <remarks>
        /// Pattern sample: Matching "c:\test\test.ps1" from pattern like ". 'c:\test\test.ps1' -param val"
        /// </remarks>
        public const string ExecutionCommandFileReplacePattern = @"(?<=\.\s\').*?(?=\')";

        #region Remote file open events

        /// <summary>
        /// Powershell script to unregister the PSEdit function
        /// </summary>
        public const string UnregisterPSEditScript = @"
if ((Test-Path -Path 'function:\\global:PSEdit') -eq $true)
{
    Remove-Item -Path 'function:\\global:PSEdit' -Force
}

Get-EventSubscriber -SourceIdentifier PSISERemoteSessionOpenFile -EA Ignore | Remove-Event
";

        /// <summary>
        /// Powershell script to register any function into runspace
        /// </summary>
        public const string RegisterPSEditScript = @"
param (
    [string] $PSEditFunction
)

    Register-EngineEvent -SourceIdentifier PSISERemoteSessionOpenFile -Forward
    if ((Test-Path -Path 'function:\\global:PSEdit') -eq $false)
    {
        Set-Item -Path 'function:\\global:PSEdit' -Value $PSEditFunction
    }
";

        /// <summary>
        /// PSEdit equivalent functionality
        /// </summary>
        public const string PSEditFunctionScript = @"
param (
    [Parameter(Mandatory=$true)] [String[]] $FileNames
)

    foreach ($fileName in $FileNames)
    {
        dir $fileName | where { ! $_.PSIsContainer } | foreach {
            $filePathName = $_.FullName
            
            # Get file contents
            $contentBytes = Get-Content -Path $filePathName -Raw -Encoding Byte
            
            # Notify client for file open.
            New-Event -SourceIdentifier PSISERemoteSessionOpenFile -EventArguments @($filePathName, $contentBytes) > $null
        }
    }
";

        /// <summary>
        /// The parameter name of the function to be registered
        /// </summary>
        public const string RegisterPSEditParameterName = "PSEditFunction";

        #endregion


        public const string ReadHostDialogTitle = "Read-Host";

        /// <summary>
        /// The default cmdlet we used to connect PowerShell remote session
        /// </summary>
        /// <remarks>
        /// {0} - remote computer name
        /// </remarks>
        public const string EnterRemoteSessionDefaultCommand = "Enter-PSSession -ComputerName {0} -Credential ''";

        public const string ExitRemoteSessionDefaultCommand = "Exit-PSSession";

        // PowerShell debugging command
        public const DebuggerResumeAction Debugger_Stop = DebuggerResumeAction.Stop;
        public const DebuggerResumeAction Debugger_StepOver = DebuggerResumeAction.StepOver;
        public const DebuggerResumeAction Debugger_StepInto = DebuggerResumeAction.StepInto;
        public const DebuggerResumeAction Debugger_StepOut = DebuggerResumeAction.StepOut;
        public const DebuggerResumeAction Debugger_Continue = DebuggerResumeAction.Continue;

        // PowerShell breakpoint command
        public const string SetPSBreakpoint = "Set-PSBreakpoint -Script \"{0}\" -Line {1}";
        public const string DisablePSBreakpoint = "Disable-PSBreakpoint -Id {0}";
        public const string EnablePSBreakpoint = "Enable-PSBreakpoint -Id {0}";
        public const string RemovePSBreakpoint = "Remove-PSBreakpoint -Id {0}";
        public const string GetPrompt = "prompt";

        // Terminating error output format
        // {0} - Exception message
        // {1} - Newline
        // {2} - CategoryInfo
        // {3} - FullyQualifiedErrorId
        public const string TerminatingErrorFormat = "[ERROR] {0}{1}[ERROR] + CategoryInfo          : {2}{1}[ERROR] + FullyQualifiedErrorId : {3}{1}";

        public const string PowerShellHostProcessLogTag = "[{0}]:";
        public const string PowerShellHostProcessLogFormat = PowerShellHostProcessLogTag + "{1}";
    }
}
