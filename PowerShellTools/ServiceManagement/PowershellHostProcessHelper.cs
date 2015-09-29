using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using PowerShellTools.Common;
using PowerShellTools.Common.Debugging;
using PowerShellTools.Common.Logging;
using PowerShellTools.Options;

namespace PowerShellTools.ServiceManagement
{
    /// <summary>
    /// Helper class for creating a process used to host the WCF service.
    /// </summary>
    internal static class PowershellHostProcessHelper
    {
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
        private const int SW_HIDE = 0;

        private static readonly ILog Log = LogManager.GetLogger(typeof(PowershellHostProcessHelper));

        private static Guid EndPointGuid { get; set; }

        public static PowerShellHostProcess CreatePowerShellHostProcess(BitnessOptions bitness)
        {
            Log.DebugFormat("Starting host process. Bitness: {0}", bitness);
            PowerShellToolsPackage.DebuggerReadyEvent.Reset();

            Process powerShellHostProcess = new Process();
            string hostProcessReadyEventName = Constants.ReadyEventPrefix + Guid.NewGuid();
            EndPointGuid = Guid.NewGuid();

            string exeName;
            switch (bitness)
            {
                case BitnessOptions.x86:
                    exeName = Constants.PowerShellHostExeNameForx86;
                    break;
                case BitnessOptions.DefaultToOperatingSystem:
                default:
                    exeName = Constants.PowerShellHostExeName;
                    break;
            }
            string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(currentPath, exeName);
            string hostArgs = String.Format(CultureInfo.InvariantCulture,
                                            "{0}{1} {2}{3} {4}{5}",
                                            Constants.UniqueEndpointArg, EndPointGuid, // For generating a unique endpoint address 
                                            Constants.VsProcessIdArg, Process.GetCurrentProcess().Id,
                                            Constants.ReadyEventUniqueNameArg, hostProcessReadyEventName);

            Log.DebugFormat("Host path: '{0}' Host arguments: '{1}'", path, hostArgs);

            powerShellHostProcess.StartInfo.Arguments = hostArgs;
            powerShellHostProcess.StartInfo.FileName = path;

            powerShellHostProcess.StartInfo.CreateNoWindow = false;
            powerShellHostProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            powerShellHostProcess.StartInfo.UseShellExecute = false;
            powerShellHostProcess.StartInfo.RedirectStandardInput = true;
            powerShellHostProcess.StartInfo.RedirectStandardOutput = true;
            powerShellHostProcess.StartInfo.RedirectStandardError = true;

            powerShellHostProcess.OutputDataReceived += PowerShellHostProcess_OutputDataReceived;
            powerShellHostProcess.ErrorDataReceived += PowerShellHostProcess_ErrorDataReceived;

            EventWaitHandle readyEvent = new EventWaitHandle(false, EventResetMode.ManualReset, hostProcessReadyEventName);

            powerShellHostProcess.Start();
            powerShellHostProcess.EnableRaisingEvents = true;

            powerShellHostProcess.BeginOutputReadLine();
            powerShellHostProcess.BeginErrorReadLine();

            // For now we dont set timeout and wait infinitely
            // Further UI work might enable some better UX like retry logic for case where remote process being unresponsive
            // By then we will bring timeout back here.
            bool success = readyEvent.WaitOne();
            readyEvent.Close();

            if (!success)
            {
                Log.Warn("Failed to start host!");
                int processId = powerShellHostProcess.Id;
                try
                {
                    powerShellHostProcess.Kill();
                }
                catch (Exception)
                {
                }

                if (powerShellHostProcess != null)
                {
                    powerShellHostProcess.Dispose();
                    powerShellHostProcess = null;
                }
                throw new PowerShellHostProcessException(String.Format(CultureInfo.CurrentCulture,
                                                                        Resources.ErrorFailToCreateProcess,
                                                                        processId.ToString()));
            }

            return new PowerShellHostProcess(powerShellHostProcess, EndPointGuid);
        }

        private static void PowerShellHostProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                PowerShellHostProcessOutput(e.Data);
            }
        }

        private static void PowerShellHostProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                PowerShellHostProcessOutput(e.Data);
            }
        }

        private static void PowerShellHostProcessOutput(string outputData)
        {
            Log.Debug(outputData);
        }
    }

    /// <summary>
    /// The structure containing the process we want and a guid used for the WCF client to establish connection to the service.
    /// </summary>
    public class PowerShellHostProcess
    {
        public Process Process
        {
            get;
            private set;
        }

        public Guid EndpointGuid
        {
            get;
            private set;
        }

        public PowerShellHostProcess(Process process, Guid guid)
        {
            Process = process;
            EndpointGuid = guid;
        }

        /// <summary>
        /// Write user input into standard input pipe(redirected)
        /// </summary>
        /// <param name="content">User input string</param>
        public void WriteHostProcessStandardInputStream(string content)
        {
            StreamWriter _inputStreamWriter = Process.StandardInput;

            // Feed into stdin stream
            _inputStreamWriter.WriteLine(content);
            _inputStreamWriter.Flush();
        }
    }
}
