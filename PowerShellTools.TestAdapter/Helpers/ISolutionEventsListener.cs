using System;
using XmlTestAdapter.EventWatchers.EventArgs;

namespace XmlTestAdapter.EventWatchers
{
    public interface ISolutionEventsListener
    {
        /// <summary>
        /// Fires an event when a project is opened/closed/loaded/unloaded
        /// </summary>
        event EventHandler<SolutionEventsListenerEventArgs> SolutionProjectChanged;

        void StartListeningForChanges();
        void StopListeningForChanges();
        event EventHandler SolutionUnloaded;
    }
}