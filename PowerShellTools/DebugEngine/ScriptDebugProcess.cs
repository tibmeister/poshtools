using System;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace PowerShellTools.DebugEngine
{
    public class ScriptDebugProcess : IDebugProcess2 
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScriptDebugProcess));

        private readonly IDebugPort2 _port;

        public ScriptDebugProcess(IDebugPort2 debugPort, uint processId) : this(debugPort)
        {
            ProcessId = processId;
        }

        public ScriptDebugProcess(IDebugPort2 debugPort)
        {
            Log.Debug("Process: Constructor");
            Id = Guid.NewGuid();
            _port = debugPort;
            Node = new ScriptProgramNode(this);
        }

        public uint ProcessId { get; set; }

        public Guid Id { get; set; }

        public ScriptProgramNode Node { get; set; }

        public int GetInfo(enum_PROCESS_INFO_FIELDS fields, PROCESS_INFO[] pProcessInfo)
        {
            Log.Debug("Process: GetInfo");

            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME) != 0)
            {
                pProcessInfo[0].bstrFileName = Node.FileName;
                pProcessInfo[0].Flags = enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED |
                                        enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING;
                pProcessInfo[0].Fields = enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME | enum_PROCESS_INFO_FIELDS.PIF_FLAGS;
            }
            return VSConstants.S_OK;
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            Log.Debug("Process: EnumPrograms");
            ppEnum = null;
            return VSConstants.S_OK;
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            Log.Debug("Process: GetName");
            pbstrName = "PowerShell Script Process";
            return VSConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            Log.Debug("Process: GetServer");
            ppServer = null;
            return VSConstants.S_OK;
        }

        public int Terminate()
        {
            Log.Debug("Process: Terminate");
            return VSConstants.S_OK;
        }

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            Log.Debug("Process: Attach");
            return VSConstants.S_OK;
        }

        public int CanDetach()
        {
            Log.Debug("Process: CanDetach");
            return VSConstants.S_OK;
        }

        public int Detach()
        {
            Log.Debug("Process: Detach");
            return VSConstants.S_OK;
        }

        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pProcessId[0].guidProcessId = Id;
            Log.Debug("Process: GetPhysicalProcessId");
            return VSConstants.S_OK;
        }

        public int GetProcessId(out Guid pguidProcessId)
        {
            Log.Debug("Process: GetProcessId");
            pguidProcessId = Id;
            return VSConstants.S_OK;
        }

        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            Log.Debug("Process: GetAttachedSessionName");
            pbstrSessionName = String.Empty;
            return VSConstants.S_OK;
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            Log.Debug("Process: EnumThreads");
            ppEnum = null;
            return VSConstants.S_OK;
        }

        public int CauseBreak()
        {
            Log.Debug("Process: CauseBreak");
            return VSConstants.S_OK;
        }

        public int GetPort(out IDebugPort2 ppPort)
        {
            Log.Debug("Process: GetPort");
            ppPort = _port;
            return VSConstants.S_OK;
        }
    }
}
