using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace PowerShellTools.Classification
{
    /// <summary>
    /// Provides error tagging for PowerShell scripts. 
    /// </summary>
    /// <remarks>
    /// This code takes advantage of the already parsed AST to gather any errors while parsing
    /// and to tag the spans that are in an error state.
    /// </remarks>
    internal class PowerShellErrorTagger : ITagger<ErrorTag>, INotifyTagsChanged
	{
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
		private ITextBuffer Buffer { get;set; }

		internal PowerShellErrorTagger(ITextBuffer sourceBuffer)
		{
			Buffer = sourceBuffer;
			Buffer.Properties.AddProperty(typeof(PowerShellErrorTagger).Name, this);
			Buffer.ContentTypeChanged += Buffer_ContentTypeChanged;
		}

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var currentSnapshot = Buffer.CurrentSnapshot;
		    if (currentSnapshot.Length == 0) yield break;

		    List<TagInformation<ErrorTag>> list;
		    Buffer.Properties.TryGetProperty(BufferProperties.TokenErrorTags, out list);

		    foreach (var tagSpan in list.Select(current => current.GetTagSpan(currentSnapshot)).Where(tagSpan => tagSpan != null))
		    {
		        yield return tagSpan;
		    }
		}

		public void OnTagsChanged(SnapshotSpan span)
		{
			var tagsChanged = TagsChanged;
			if (tagsChanged != null)
			{
				tagsChanged(this, new SnapshotSpanEventArgs(span));
			}
		}

		private void Buffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
		{
			Buffer.ContentTypeChanged -= Buffer_ContentTypeChanged;
			Buffer.Properties.RemoveProperty(typeof(PowerShellErrorTagger).Name);
		}
	}
}
