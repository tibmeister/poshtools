using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.Common.ServiceManagement.ExplorerContract
{
    [ServiceContract(CallbackContract = typeof(IPowerShellExplorerServiceCallback))]
    public interface IPowerShellExplorerService
    {
        [OperationContract]
        [ServiceKnownType(typeof(PowerShellModule))]
        Task<List<IPowerShellModule>> GetModules();

        [OperationContract]
        [ServiceKnownType(typeof(PowerShellCommand))]
        Task<List<IPowerShellCommand>> GetCommands();

        [OperationContract]
        [ServiceKnownType(typeof(PowerShellCommand))]
        Task<string> GetCommandHelp(IPowerShellCommand command);

        [OperationContract]
        [ServiceKnownType(typeof(PowerShellCommand))]
        [ServiceKnownType(typeof(PowerShellCommandMetadata))]
        [ServiceKnownType(typeof(PowerShellParameterMetadata))]
        [ServiceKnownType(typeof(PowerShellParameterSetMetadata))]
        Task<IPowerShellCommandMetadata> GetCommandMetadata(IPowerShellCommand command);
    }
}
