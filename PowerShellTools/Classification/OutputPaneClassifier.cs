using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    /// <summary>
    /// Defines classifications for the output pane.
    /// </summary>
    public class OutputPaneClassifier
    {
        public const string LogError = "PowerShellLogError";
        public const string LogWarning = "PowerShellLogWarning";
        public const string LogDebug = "PowerShellLogDebug";
        public const string LogVerbose = "PowerShellLogVerbose";

        [Export]
        [Name(LogError)]
        public static ClassificationTypeDefinition LogErrorDefinition { get; set; }

        [Name(LogError)]
        [UserVisible(true)]
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = LogError)]
        public sealed class LogErrorFormat : ClassificationFormatDefinition
        {
            public LogErrorFormat()
            {
                DisplayName =  "PowerShell Output Error";
                ForegroundColor = Colors.Red;
            }
        }

        [Export]
        [Name(LogWarning)]
        public static ClassificationTypeDefinition LogWarningDefinition { get; set; }

        [Name(LogWarning)]
        [UserVisible(true)]
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = LogWarning)]
        public sealed class LogWarningFormat : ClassificationFormatDefinition
        {
            public LogWarningFormat()
            {
                DisplayName = "PowerShell Output Warning";
                ForegroundColor = Colors.Yellow;
            }
        }

        [Export]
        [Name(LogDebug)]
        public static ClassificationTypeDefinition LogDebugDefinition { get; set; }

        [Name(LogDebug)]
        [UserVisible(true)]
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = LogDebug)]
        public sealed class LogDebugFormat : ClassificationFormatDefinition
        {
            public LogDebugFormat()
            {
                DisplayName = "PowerShell Output Debug";
                ForegroundColor = Colors.Green;
            }
        }

        [Export]
        [Name(LogVerbose)]
        public static ClassificationTypeDefinition LogVerboseDefinition { get; set; }

        [Name(LogVerbose)]
        [UserVisible(true)]
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = LogVerbose)]
        public sealed class LogVerboseFormat : ClassificationFormatDefinition
        {
            public LogVerboseFormat()
            {
                DisplayName = "PowerShell Output Verbose";
                ForegroundColor = Colors.Green;
            }
        }
    }

    /// <summary>
    /// Responsbile for classifying the output pane.
    /// </summary>
    public class OutputClassifier : IClassifier
    {
        private readonly IClassificationTypeRegistryService _classificationTypeRegistry;
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public OutputClassifier(IClassificationTypeRegistryService registry)
        {
             _classificationTypeRegistry = registry;

            // Just to avoid a compiler warning
            var temp = this.ClassificationChanged;
        }   

        /// <summary>
        /// Returns classifications for the given span.
        /// </summary>
        /// <remarks>
        /// This method looks for lines that start with the standard tags for various types of output.
        /// These include [DEBUG], [ERROR], [WARNING] and [VERBOSE]
        /// </remarks>
        /// <param name="span"></param>
        /// <returns></returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var spans = new List<ClassificationSpan>();
            var snapshot = span.Snapshot;
            if (snapshot == null || snapshot.Length == 0)
            {
                return spans;
            }

            var start = span.Start.GetContainingLine().LineNumber;
            var end = (span.End - 1).GetContainingLine().LineNumber;
            for (var i = start; i <= end; i++)
            {
                var line = snapshot.GetLineFromLineNumber(i);
                var snapshotSpan = new SnapshotSpan(line.Start, line.Length);
                var text = line.Snapshot.GetText(snapshotSpan);
                if (string.IsNullOrEmpty(text) == false)
                {
                    string classificationName = null;
                    if (text.StartsWith("[WARNING]"))
                    {
                        classificationName = OutputPaneClassifier.LogWarning;
                    }
                    else if (text.StartsWith("[ERROR]"))
                    {
                        classificationName = OutputPaneClassifier.LogError;
                    }
                    else if (text.StartsWith("[DEBUG]"))
                    {
                        classificationName = OutputPaneClassifier.LogDebug;
                    }
                    else if (text.StartsWith("[VERBOSE]"))
                    {
                        classificationName = OutputPaneClassifier.LogVerbose;
                    }

                    if (!String.IsNullOrEmpty(classificationName))
                    {
                        var type = _classificationTypeRegistry.GetClassificationType(classificationName);

                        if (type != null)
                            spans.Add(new ClassificationSpan(line.Extent, type));    
                    }
                    
                }
            }
            return spans;
        }

    }

#pragma warning disable 0649

       [ContentType("output")]
       [Export(typeof(IClassifierProvider))]
       public class OutputClassifierProvider : IClassifierProvider
       {
           [Import]
           internal IClassificationTypeRegistryService ClassificationRegistry;

           [Import]
           internal SVsServiceProvider ServiceProvider;

           public static OutputClassifier OutputClassifier { get; private set; }

           public IClassifier GetClassifier(ITextBuffer buffer)
           {
                if (OutputClassifier == null)
                {
                    OutputClassifier = new OutputClassifier(ClassificationRegistry);
                }

               return OutputClassifier;
           }
       }
}

#pragma warning restore 0649