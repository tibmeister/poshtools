using System;
using XmlTestAdapter.EventWatchers.EventArgs;

namespace XmlTestAdapter.EventWatchers
{
    public interface ITestFileAddRemoveListener
    {
        event EventHandler<TestFileChangedEventArgs> TestFileChanged;
        void StartListeningForTestFileChanges();
        void StopListeningForTestFileChanges();
    }
}