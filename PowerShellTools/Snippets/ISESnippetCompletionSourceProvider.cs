using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
namespace Microsoft.Windows.PowerShell.Gui.Internal
{
	[ContentType("powershell"), ContentType("TextOutput"), Name("powershell snippet completion"), Export(typeof(ICompletionSourceProvider))]
	internal class ISESnippetCompletionSourceProvider : ICompletionSourceProvider
	{
		private static ISESnippetCompletionSource iseSnippetCompletionSource;
		public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
		{
			if (ISESnippetCompletionSourceProvider.iseSnippetCompletionSource == null)
			{
				ISESnippetCompletionSourceProvider.iseSnippetCompletionSource = new ISESnippetCompletionSource();
			}
			return ISESnippetCompletionSourceProvider.iseSnippetCompletionSource;
		}
	}
}
