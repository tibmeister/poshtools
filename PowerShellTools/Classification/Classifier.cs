using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    internal abstract class Classifier : IClassifier, INotifyTagsChanged
	{
		[BaseDefinition("text"), Name("PS1ScriptGaps"), Export(typeof(ClassificationTypeDefinition))]
		private static ClassificationTypeDefinition scriptGapsTypeDefinition;
		private static IClassificationType scriptGaps;
		private readonly ITextBuffer buffer;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
		protected static IClassificationType ScriptGaps
		{
			get
			{
				if (scriptGaps == null)
				{
					scriptGaps = EditorImports.ClassificationTypeRegistryService.GetClassificationType("PS1ScriptGaps");
				}
				return scriptGaps;
			}
		}

		protected ITextBuffer Buffer
		{
			get
			{
				return buffer;
			}
		}
        internal Classifier(ITextBuffer buffer)
		{
			this.buffer = buffer;
		}
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			UpdateClassifierBufferProperty();
			return VirtualGetClassificationSpans(span);
		}

		internal static void SetClassificationTypeColors<T>(IDictionary<T, Color> tokenColors, IDictionary<T, Color> defaultTokenColors, string prefix, string sufix)
		{
            var classificationFormatMap = EditorImports.ClassificationFormatMap.GetClassificationFormatMap("PowerShell");
            foreach (var current in defaultTokenColors)
            {
                var classificationTypeRegistryService = EditorImports.ClassificationTypeRegistryService;
                var key = current.Key;
                var classificationType = classificationTypeRegistryService.GetClassificationType(prefix + key + sufix);
                if (classificationType == null) continue;

                var textFormattingRunProperties = classificationFormatMap.GetTextProperties(classificationType);
                Color foreground;
                if (tokenColors.TryGetValue(current.Key, out foreground))
                {
                    textFormattingRunProperties = textFormattingRunProperties.SetForeground(foreground);
                }
                else
                {
                    textFormattingRunProperties = textFormattingRunProperties.ClearForegroundBrush();
                }
                textFormattingRunProperties = textFormattingRunProperties.ClearFontRenderingEmSize();
                textFormattingRunProperties = textFormattingRunProperties.ClearTypeface();
                classificationFormatMap.SetTextProperties(classificationType, textFormattingRunProperties);
            }
		}

	    internal static void SetFontColor(Color color, IClassificationType classificationType, string category)
		{
			var classificationFormatMap = EditorImports.ClassificationFormatMap.GetClassificationFormatMap(category);
			var textFormattingRunProperties = classificationFormatMap.GetTextProperties(classificationType);
			textFormattingRunProperties = textFormattingRunProperties.SetForeground(color);
			classificationFormatMap.SetTextProperties(classificationType, textFormattingRunProperties);
		}
		internal static TextFormattingRunProperties GetTextProperties(IClassificationType type, string category)
		{
			var classificationFormatMap = EditorImports.ClassificationFormatMap.GetClassificationFormatMap(category);
			return classificationFormatMap.GetTextProperties(type);
		}

		public void OnTagsChanged(SnapshotSpan notificationSpan)
		{
			var classificationChanged = ClassificationChanged;
			if (classificationChanged != null)
			{
				classificationChanged(this, new ClassificationChangedEventArgs(notificationSpan));
			}
		}

		protected abstract IList<ClassificationSpan> VirtualGetClassificationSpans(SnapshotSpan span);
		private void UpdateClassifierBufferProperty()
		{
			Classifier classifier;
			if (buffer.Properties.TryGetProperty(typeof(Classifier).Name, out classifier))
			{
			    if (classifier == this) return;
			    buffer.Properties.RemoveProperty(typeof(Classifier).Name);
			    buffer.Properties.AddProperty(typeof(Classifier).Name, this);
			}
			else
			{
				buffer.Properties.AddProperty(typeof(Classifier).Name, this);
			}
		}
	}
}
