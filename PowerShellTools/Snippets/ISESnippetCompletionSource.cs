using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
namespace Microsoft.Windows.PowerShell.Gui.Internal
{
	internal class ISESnippetCompletionSource : ICompletionSource, IDisposable
	{
		private class CompletionComparer : IComparer<Completion>
		{
			public int Compare(Completion leftCompletion, Completion rightCompletion)
			{
				return string.Compare(leftCompletion.DisplayText, rightCompletion.DisplayText, StringComparison.OrdinalIgnoreCase);
			}
		}
		private static ISESnippetCompletionSource.CompletionComparer completionComparer = new ISESnippetCompletionSource.CompletionComparer();
		public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			if (session.Properties.ContainsProperty("SessionOrigin_Intellisense"))
			{
				return;
			}
			List<Completion> list = PSGInternalHost.Current.Options.ShowDefaultSnippets ? PSGInternalHost.Current.PowerShellTabs.SelectedPowerShellTab.Snippets.SnippetCompletions : PSGInternalHost.Current.PowerShellTabs.SelectedPowerShellTab.Snippets.NonDefaultSnippetCompletions;
			list.Sort(ISESnippetCompletionSource.completionComparer);
			ITrackingPoint triggerPoint = session.GetTriggerPoint(session.TextView.TextBuffer);
			ITextSnapshot currentSnapshot = session.TextView.TextBuffer.CurrentSnapshot;
			ITrackingSpan trackingSpan = currentSnapshot.CreateTrackingSpan(triggerPoint.GetPosition(session.TextView.TextBuffer.CurrentSnapshot), 0, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Backward);
			ISECompletionSet item = new ISECompletionSet(string.Empty, string.Empty, trackingSpan, list, null, trackingSpan, null, true);
			completionSets.Add(item);
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}
