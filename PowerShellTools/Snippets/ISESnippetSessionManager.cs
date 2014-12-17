using Microsoft.PowerShell.Host.ISE;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Text;
using PowerShellTools;

namespace Microsoft.Windows.PowerShell.Gui.Internal
{
	internal static class ISESnippetSessionManager
	{
		private static ICompletionSession activeSession;
		private static bool canFilter;
		private static ISESnippet selectedSnippet;
		private static ITrackingSpan insertSpan;
		private static EventHandler eventHandlerSessionCommitted = new EventHandler(ISESnippetSessionManager.OnActiveSessionCommitted);
		private static EventHandler eventHandlerSessionDismissed = new EventHandler(ISESnippetSessionManager.OnActiveSessionDismissed);
		private static EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> eventHandlerSelectionChanged = new EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>>(ISESnippetSessionManager.SelectedCompletionSet_SelectionStatusChanged);
		internal static ICompletionSession ActiveSession
		{
			get
			{
				return ISESnippetSessionManager.activeSession;
			}
		}
		internal static bool ShowCompletion(ITextView view)
		{
			bool result = false;
			if (ISESnippetSessionManager.activeSession != null)
			{
				ISESnippetSessionManager.activeSession.Dismiss();
			}
			EditorImports.CompletionBroker.DismissAllSessions(view);
			if (!CommandImplementation.CanShowSnippet())
			{
				return result;
			}
			ISESnippetSessionManager.activeSession = EditorImports.CompletionBroker.TriggerCompletion(view);
			if (ISESnippetSessionManager.activeSession == null)
			{
				return result;
			}
			ISESnippetSessionManager.activeSession.Committed += ISESnippetSessionManager.eventHandlerSessionCommitted;
			ISESnippetSessionManager.activeSession.Dismissed += ISESnippetSessionManager.eventHandlerSessionDismissed;
			ISESnippetSessionManager.activeSession.SelectedCompletionSet.SelectionStatusChanged += ISESnippetSessionManager.eventHandlerSelectionChanged;
			ISESnippetSessionManager.activeSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(ISESnippetSessionManager.activeSession.SelectedCompletionSet.Completions[0], true, true);
			ISESnippetSessionManager.selectedSnippet = (ISESnippetSessionManager.activeSession.SelectedCompletionSet.SelectionStatus.Completion.Properties["SnippetInfo"] as ISESnippet);
			ISESnippetSessionManager.insertSpan = ISESnippetSessionManager.activeSession.SelectedCompletionSet.ApplicableTo;
			ISESnippetSessionManager.canFilter = true;
			return true;
		}
		internal static bool CommitCompletion()
		{
			bool result = false;
			if (ISESnippetSessionManager.activeSession == null)
			{
				return result;
			}
			ISESnippetSessionManager.canFilter = false;
			ISESnippetSessionManager.activeSession.Commit();
			return true;
		}
		internal static void Filter()
		{
			if (!ISESnippetSessionManager.canFilter)
			{
				return;
			}
			ISESnippetSessionManager.activeSession.Filter();
		}
		private static string GetIndentationPrependText(SnapshotPoint startPoint)
		{
			if (startPoint.Position <= startPoint.GetContainingLine().Start.Position)
			{
				return string.Empty;
			}
			int num = startPoint.Position - startPoint.GetContainingLine().Start.Position;
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < num; i++)
			{
				stringBuilder.Append(' ');
			}
			return stringBuilder.ToString();
		}
		private static void OnActiveSessionCommitted(object sender, EventArgs e)
		{
			ITextView textView = (sender as ICompletionSession).TextView;
			SnapshotPoint startPoint = ISESnippetSessionManager.insertSpan.GetStartPoint(textView.TextBuffer.CurrentSnapshot);
			SnapshotPoint endPoint = ISESnippetSessionManager.insertSpan.GetEndPoint(textView.TextBuffer.CurrentSnapshot);
			if (ISESnippetSessionManager.selectedSnippet.CaretOffsetFromStart >= 0)
			{
				if (endPoint.Position - startPoint.Position < ISESnippetSessionManager.selectedSnippet.CaretOffsetFromStart)
				{
					return;
				}
				textView.Caret.MoveTo(startPoint.Add(ISESnippetSessionManager.selectedSnippet.CaretOffsetFromStart));
			}
			if (ISESnippetSessionManager.selectedSnippet.Indent && startPoint.Position > startPoint.GetContainingLine().Start.Position)
			{
				string indentationPrependText = ISESnippetSessionManager.GetIndentationPrependText(startPoint);
				string text = ISESnippetSessionManager.insertSpan.GetText(textView.TextBuffer.CurrentSnapshot);
				Stack<int> stack = new Stack<int>();
				int num;
				for (int i = 0; i < text.Length; i = num + 2)
				{
					num = text.IndexOf("\r\n", i, StringComparison.Ordinal);
					if (num == -1)
					{
						break;
					}
					stack.Push(num + 2);
				}
				ITextEdit textEdit = textView.TextBuffer.CreateEdit();
				while (stack.Count > 0)
				{
					textEdit.Insert(stack.Pop() + startPoint.Position, indentationPrependText);
				}
				textEdit.Apply();
			}
			if (ISESnippetSessionManager.activeSession != null)
			{
				ISESnippetSessionManager.activeSession.Committed -= ISESnippetSessionManager.eventHandlerSessionCommitted;
				ISESnippetSessionManager.activeSession.SelectedCompletionSet.SelectionStatusChanged -= ISESnippetSessionManager.eventHandlerSelectionChanged;
			}
		}
		private static void OnActiveSessionDismissed(object sender, EventArgs e)
		{
			ISESnippetSessionManager.activeSession.Dismissed -= ISESnippetSessionManager.eventHandlerSessionDismissed;
			ISESnippetSessionManager.activeSession.SelectedCompletionSet.SelectionStatusChanged -= ISESnippetSessionManager.eventHandlerSelectionChanged;
			ISESnippetSessionManager.activeSession = null;
		}
		private static void SelectedCompletionSet_SelectionStatusChanged(object sender, ValueChangedEventArgs<CompletionSelectionStatus> e)
		{
			if (e.NewValue.Completion != null)
			{
				ISESnippetSessionManager.selectedSnippet = (e.NewValue.Completion.Properties["SnippetInfo"] as ISESnippet);
			}
		}
	}
}
