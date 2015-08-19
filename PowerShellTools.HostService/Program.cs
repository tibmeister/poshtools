using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.ServiceModel;
using System.Threading;
using PowerShellTools.Common;
using PowerShellTools.Common.ServiceManagement.IntelliSenseContract;
using PowerShellTools.HostService.ServiceManagement;
using PowerShellTools.Common.ServiceManagement.DebuggingContract;
using PowerShellTools.HostService.ServiceManagement.Debugging;

namespace PowerShellTools.HostService
{
    internal class Program
    {
        private static ServiceHost _powershellServiceHost;
        private static ServiceHost _powershellDebuggingServiceHost;
        private static AutoResetEvent _processExitEvent;

        public static int VsProcessId { get; private set; }

        public static string EndpointGuid { get; private set; }

        [LoaderOptimization(LoaderOptimization.SingleDomain)]
        internal static int Main(string[] args)
        {
            if (args.Length != 3 ||
                !(args[0].StartsWith(Constants.UniqueEndpointArg, StringComparison.OrdinalIgnoreCase)
                && args[1].StartsWith(Constants.VsProcessIdArg, StringComparison.OrdinalIgnoreCase)
                && args[2].StartsWith(Constants.ReadyEventUniqueNameArg, StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }

            _processExitEvent = new AutoResetEvent(false);

            EndpointGuid = args[0].Remove(0, Constants.UniqueEndpointArg.Length);
            if (EndpointGuid.Length != Guid.Empty.ToString().Length)
            {
                return 1;
            }

            int vsProcessId;
            if (!Int32.TryParse(args[1].Remove(0, Constants.VsProcessIdArg.Length),
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out vsProcessId))
            {
                return 1;
            }

            VsProcessId = vsProcessId;

            string readyEventName = args[2].Remove(0, Constants.ReadyEventUniqueNameArg.Length);
            // the readyEventName should be VsPowershellToolProcess:TheGeneratedGuid
            if (readyEventName.Length != (Constants.ReadyEventPrefix.Length + Guid.Empty.ToString().Length))
            {
                return 1;
            }

            // Step 1: Create the NetNamedPipeBinding. 
            // Note: the setup of the binding should be same as the client side, otherwise, the connection won't get established
            Uri baseAddress = new Uri(Constants.ProcessManagerHostUri + EndpointGuid);
            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            binding.ReceiveTimeout = TimeSpan.MaxValue;
            binding.SendTimeout = TimeSpan.MaxValue;
            binding.Security.Transport.ProtectionLevel = ProtectionLevel.None;
            binding.MaxReceivedMessageSize = Constants.BindingMaxReceivedMessageSize;

            // Step 2: Create the service host.
            CreatePowershellIntelliSenseServiceHost(baseAddress, binding);
            CreatePowershellDebuggingServiceHost(baseAddress, binding);

            // Step 3: Signal parent process that host is ready so that it can proceed.
            EventWaitHandle readyEvent = new EventWaitHandle(false, EventResetMode.ManualReset, readyEventName);
            readyEvent.Set();
            readyEvent.Close();

            try
            {
                // get parent process (The VS process)
                Process p = Process.GetProcessById(vsProcessId);

                if (p != null)
                {
                    p.EnableRaisingEvents = true;
                    // Make sure the host process terminates when VS exits.
                    p.Exited += new EventHandler(
                        (sender, eventArgs) =>
                        {
                            if (_powershellServiceHost != null)
                            {
                                _powershellServiceHost.Close();
                                _powershellServiceHost = null;
                            }
                            if (_powershellDebuggingServiceHost != null)
                            {
                                _powershellDebuggingServiceHost.Close();
                                _powershellDebuggingServiceHost = null;
                            }

                            _processExitEvent.Set();
                        });
                }

                _processExitEvent.WaitOne();
            }
            catch (Exception)
            {
                // The process need to wait for the parent process to exit.  
            }

            if (_powershellServiceHost != null)
            {
                _powershellServiceHost.Close();
                _powershellServiceHost = null;
            }

            if (_powershellDebuggingServiceHost != null)
            {
                _powershellDebuggingServiceHost.Close();
                _powershellDebuggingServiceHost = null;
            }

            Environment.Exit(0);
            return 0;
        }

        private static void CreatePowershellIntelliSenseServiceHost(Uri baseAddress, NetNamedPipeBinding binding)
        {
            _powershellServiceHost = new ServiceHost(typeof(PowerShellIntelliSenseService), baseAddress);

            _powershellServiceHost.AddServiceEndpoint(typeof(IPowershellIntelliSenseService),
                                                      binding,
                                                      Constants.IntelliSenseHostRelativeUri);

            _powershellServiceHost.Open();
        }

        private static void CreatePowershellDebuggingServiceHost(Uri baseAddress, NetNamedPipeBinding binding)
        {
            _powershellDebuggingServiceHost = new ServiceHost(typeof(PowerShellDebuggingService), baseAddress);

            _powershellDebuggingServiceHost.AddServiceEndpoint(typeof(IPowershellDebuggingService),
                binding,
                Constants.DebuggingHostRelativeUri);

            _powershellDebuggingServiceHost.Open();
        }
    }
}
