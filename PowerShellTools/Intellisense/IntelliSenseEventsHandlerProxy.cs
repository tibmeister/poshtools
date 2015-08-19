using System;
using System.ServiceModel;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PowerShellTools.Common.ServiceManagement.IntelliSenseContract;

namespace PowerShellTools.Intellisense
{
    /// <summary>
    /// Proxy of intellisense service event handlers
    /// This works as InstanceContext for intellisense service channel
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class IntelliSenseEventsHandlerProxy : IIntelliSenseServiceCallback
    {
        // An event for receving completion list from remote service
        public event EventHandler<EventArgs<CompletionResultList, int>> CompletionListUpdated;
        
        /// <summary>
        /// Push completion list result back to client
        /// </summary>
        /// <param name="completionResultList">Completion list got from intellisense service</param>
        public void PushCompletionResult(CompletionResultList completionResultList, int requestWindowId)
        {
            if (CompletionListUpdated != null)
            {
                CompletionListUpdated(this, new EventArgs<CompletionResultList, int>(completionResultList, requestWindowId));
            }
        }
    }
}
