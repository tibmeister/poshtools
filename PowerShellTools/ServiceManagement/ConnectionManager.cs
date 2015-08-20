using System;
using System.Diagnostics;
using System.ServiceModel;
using log4net;
using PowerShellTools.Common;
using PowerShellTools.Common.ServiceManagement.DebuggingContract;
using PowerShellTools.Common.ServiceManagement.IntelliSenseContract;
using PowerShellTools.DebugEngine;

namespace PowerShellTools.ServiceManagement
{
    /// <summary>
    /// Manage the process and channel creation.
    /// </summary>
    internal sealed class ConnectionManager
    {
        private IPowershellIntelliSenseService _powershellIntelliSenseService;
        private IPowershellDebuggingService _powershellDebuggingService;
        private object _syncObject = new object();
        private static object _staticSyncObject = new object();
        private static ConnectionManager _instance;
        private Process _process;
        private ChannelFactory<IPowershellIntelliSenseService> _intelliSenseServiceChannelFactory;
        private ChannelFactory<IPowershellDebuggingService> _debuggingServiceChannelFactory;
        private static readonly ILog Log = LogManager.GetLogger(typeof(PowerShellToolsPackage));
        private PowerShellHostProcess _hostProcess;

        /// <summary>
        /// Event is fired when the connection exception happened.
        /// </summary>
        public event EventHandler ConnectionException;

        private ConnectionManager()
        {
            OpenClientConnection();
        }

        /// <summary>
        /// Connection manager instance.
        /// </summary>
        public static ConnectionManager Instance
        {
            get
            {
                lock (_staticSyncObject)
                {
                    if (_instance == null)
                    {
                        _instance = new ConnectionManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// The IntelliSense service channel.
        /// </summary>
        public IPowershellIntelliSenseService PowershellIntelliSenseSerivce
        {
            get
            {
                if (_powershellIntelliSenseService == null)
                {
                    OpenClientConnection();
                }

                return _powershellIntelliSenseService;
            }
        }

        /// <summary>
        /// The debugging service channel.
        /// </summary>
        public IPowershellDebuggingService PowershellDebuggingService
        {
            get
            {
                if (_powershellDebuggingService == null)
                {
                    OpenClientConnection();
                }
                return _powershellDebuggingService;
            }
        }

        /// <summary>
        /// PowerShell host process
        /// </summary>
        public PowerShellHostProcess HostProcess
        {
            get
            {
                return _hostProcess;
            }
        }

        private void OpenClientConnection()
        {
            lock (_syncObject)
            {
                if (_powershellIntelliSenseService == null || _powershellDebuggingService == null)
                {
                    EnsureCloseProcess(_process);
                    _hostProcess = PowershellHostProcessHelper.CreatePowershellHostProcess();
                    _process = _hostProcess.Process;
                    _process.Exited += ConnectionExceptionHandler;

                    // net.pipe://localhost/UniqueEndpointGuid/{RelativeUri}
                    var intelliSenseServiceEndPointAddress = Constants.ProcessManagerHostUri + _hostProcess.EndpointGuid + "/" + Constants.IntelliSenseHostRelativeUri;
                    var deubggingServiceEndPointAddress = Constants.ProcessManagerHostUri + _hostProcess.EndpointGuid + "/" + Constants.DebuggingHostRelativeUri;

                    try
                    {
                        _intelliSenseServiceChannelFactory = ChannelFactoryHelper.CreateDuplexChannelFactory<IPowershellIntelliSenseService>(intelliSenseServiceEndPointAddress,  new InstanceContext(PowerShellToolsPackage.Instance.IntelliSenseServiceContext));
                        _intelliSenseServiceChannelFactory.Faulted += ConnectionExceptionHandler;
                        _intelliSenseServiceChannelFactory.Closed += ConnectionExceptionHandler;
                        _intelliSenseServiceChannelFactory.Open();
                        _powershellIntelliSenseService = _intelliSenseServiceChannelFactory.CreateChannel();

                        _debuggingServiceChannelFactory = ChannelFactoryHelper.CreateDuplexChannelFactory<IPowershellDebuggingService>(deubggingServiceEndPointAddress, new InstanceContext(new DebugServiceEventsHandlerProxy()));
                        _debuggingServiceChannelFactory.Faulted += ConnectionExceptionHandler;
                        _debuggingServiceChannelFactory.Closed += ConnectionExceptionHandler;
                        _debuggingServiceChannelFactory.Open();
                        _powershellDebuggingService = _debuggingServiceChannelFactory.CreateChannel();
                        _powershellDebuggingService.SetRunspace(PowerShellToolsPackage.OverrideExecutionPolicyConfiguration);
                    }
                    catch
                    {
                        // Connection has to be established...
                        Log.Error("Connection establish failed...");

                        _powershellIntelliSenseService = null;
                        _powershellDebuggingService = null;
                        throw;
                    }
                }
            }
        }

        private void ConnectionExceptionHandler(object sender, EventArgs e)
        {
            PowerShellToolsPackage.DebuggerReadyEvent.Reset();

            EnsureClearServiceChannel();

            if (ConnectionException != null)
            {
                ConnectionException(this, EventArgs.Empty);
            }
        }

        private void EnsureCloseProcess(Process process)
        {
            if (process != null)
            {
                try
                {
                    process.Kill();
                    process = null;
                }
                catch
                {
                    //TODO: log excetion info here
                }
            }
        }

        private void EnsureClearServiceChannel()
        {
            if (_intelliSenseServiceChannelFactory != null)
            {
                _intelliSenseServiceChannelFactory.Abort();
                _intelliSenseServiceChannelFactory = null;
                _powershellIntelliSenseService = null;
            }

            if (_debuggingServiceChannelFactory != null)
            {
                _debuggingServiceChannelFactory.Abort();
                _debuggingServiceChannelFactory = null;
                _powershellDebuggingService = null;
            }

        }
    }
}
