using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudioTools;
using PowerShellTools.Common.Logging;
using PowerShellTools.LanguageService;
using Tasks = System.Threading.Tasks;

namespace PowerShellTools.Classification
{
    internal class PowerShellTokenizationService : IPowerShellTokenizationService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PowerShellTokenizationService));
        private readonly object _tokenizationLock = new object();
        private static readonly IEnumerable<string> TaskIdentifiers = new [] {"#TODO", "#HACK", "#BUG"};
        public event EventHandler<Ast> TokenizationComplete;

        private readonly ClassifierService _classifierService;
        private readonly ErrorTagSpanService _errorTagService;
        private readonly RegionAndBraceMatchingService _regionAndBraceMatchingService;

        private ITextBuffer _textBuffer;
        private ITextSnapshot _lastSnapshot;
        private static bool _isBufferTokenizing;
        private static TodoWindowTaskProvider taskProvider;
        private static ErrorListProvider errorListProvider;

        private static void CreateProvider()
        {
            if (taskProvider == null)
            {
                taskProvider = new TodoWindowTaskProvider(PowerShellToolsPackage.Instance);
                taskProvider.ProviderName = "To Do";
            }

            if (errorListProvider == null)
            {
                errorListProvider = new ErrorListProvider(PowerShellToolsPackage.Instance);
            }
        }

        public PowerShellTokenizationService(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _classifierService = new ClassifierService();
            _errorTagService = new ErrorTagSpanService();
            _regionAndBraceMatchingService = new RegionAndBraceMatchingService();

            _isBufferTokenizing = true;
            _lastSnapshot = _textBuffer.CurrentSnapshot;
            UpdateTokenization();
        }

        public void StartTokenization()
        {
            lock (_tokenizationLock)
            {
                if (_lastSnapshot == null ||
                    (_lastSnapshot.Version.VersionNumber != _textBuffer.CurrentSnapshot.Version.VersionNumber &&
                    _textBuffer.CurrentSnapshot.Length > 0))
                {
                    if (!_isBufferTokenizing)
                    {
                        _isBufferTokenizing = true;

                        Tasks.Task.Factory.StartNew(() =>
                        {
                            UpdateTokenization();
                        });
                    }
                }
            }
        }

        private void UpdateTokenization()
        {
            while (true)
            {
                var currentSnapshot = _textBuffer.CurrentSnapshot;
                try
                {
                    string scriptToTokenize = currentSnapshot.GetText();

                    Ast genereatedAst;
                    Token[] generatedTokens;
                    List<ClassificationInfo> tokenSpans;
                    List<TagInformation<ErrorTag>> errorTags;
                    Dictionary<int, int> startBraces;
                    Dictionary<int, int> endBraces;
                    List<TagInformation<IOutliningRegionTag>> regions;
                    Tokenize(currentSnapshot, scriptToTokenize, 0, out genereatedAst, out generatedTokens, out tokenSpans, out errorTags, out startBraces, out endBraces, out regions);

                    lock (_tokenizationLock)
                    {
                        if (_textBuffer.CurrentSnapshot.Version.VersionNumber == currentSnapshot.Version.VersionNumber)
                        {
                            Tasks.Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    UpdateErrorList(errorTags, currentSnapshot);
                                    UpdateTaskList(generatedTokens, currentSnapshot);
                                }
                                catch (Exception ex)
                                {
                                    Log.Warn("Failed to update task list!", ex);
                                }
                            });

                            SetTokenizationProperties(genereatedAst, generatedTokens, tokenSpans, errorTags, startBraces, endBraces, regions);
                            RemoveCachedTokenizationProperties();
                            _isBufferTokenizing = false;
                            _lastSnapshot = currentSnapshot;
                            OnTokenizationComplete(genereatedAst);
                            NotifyOnTagsChanged(BufferProperties.Classifier, currentSnapshot);
                            NotifyOnTagsChanged(BufferProperties.ErrorTagger, currentSnapshot);
                            NotifyOnTagsChanged(typeof(PowerShellOutliningTagger).Name, currentSnapshot);
                            NotifyBufferUpdated();
                            break;
                        }
                    }


                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to tokenize the new snapshot.", ex);
                }
            }
        }

        private static void UpdateErrorList(IEnumerable<TagInformation<ErrorTag>> errorTags, ITextSnapshot currentSnapshot)
        {
            CreateProvider();
            errorListProvider.Tasks.Clear();
            foreach (var error in errorTags)
            {
                var errorTask = new ErrorTask();
                errorTask.Text = error.Tag.ToolTipContent.ToString();
                errorTask.ErrorCategory = TaskErrorCategory.Error;
                errorTask.Line = error.Start;
                errorTask.Document = currentSnapshot.TextBuffer.GetFilePath();
                errorTask.Priority = TaskPriority.High;

                errorListProvider.Tasks.Add(errorTask);
                errorTask.Navigate += (sender, args) =>
                {
                    var dte = PowerShellToolsPackage.Instance.GetService(typeof(DTE)) as DTE;
                    var document = dte.Documents.Open(currentSnapshot.TextBuffer.GetFilePath());
                    document.Activate();
                    var ts = dte.ActiveDocument.Selection as TextSelection;
                    ts.GotoLine(error.Start, true);
                };
            }
        }

        private static void UpdateTaskList(IEnumerable<Token> generatedTokens, ITextSnapshot currentSnapshot)
        {
            CreateProvider();
            taskProvider.Tasks.Clear();
            foreach (
                var token in
                    generatedTokens.Where(
                        m => m.Kind == TokenKind.Comment && TaskIdentifiers.Any(x => m.Text.StartsWith(x, StringComparison.OrdinalIgnoreCase))))
            {
                var errorTask = new Task();
                errorTask.CanDelete = false;
                errorTask.Category = TaskCategory.Comments;
                errorTask.Document = currentSnapshot.TextBuffer.GetFilePath();
                errorTask.Line = token.Extent.StartLineNumber;
                errorTask.Column = token.Extent.StartColumnNumber;
                errorTask.Navigate += (sender, args) =>
                {
                    var dte = PowerShellToolsPackage.Instance.GetService(typeof (DTE)) as DTE;
                    var document = dte.Documents.Open(currentSnapshot.TextBuffer.GetFilePath());
                    document.Activate();
                    var ts = dte.ActiveDocument.Selection as TextSelection;
                    ts.GotoLine(token.Extent.StartLineNumber, true);
                };

                errorTask.Text = token.Text.Substring(1);
                errorTask.Priority = TaskPriority.Normal;
                errorTask.IsPriorityEditable = true;

                taskProvider.Tasks.Add(errorTask);
            }
            
            var taskList = PowerShellToolsPackage.Instance.GetService(typeof (SVsTaskList)) as IVsTaskList2;
            if (taskList == null)
            {
                return;
            }

            var guidProvider = typeof (TodoWindowTaskProvider).GUID;
            taskList.SetActiveProvider(ref guidProvider);
        }

        private void NotifyOnTagsChanged(string name, ITextSnapshot currentSnapshot)
        {
            INotifyTagsChanged classifier;
            if (_textBuffer.Properties.TryGetProperty<INotifyTagsChanged>(name, out classifier))
            {
                classifier.OnTagsChanged(new SnapshotSpan(currentSnapshot, new Span(0, currentSnapshot.Length)));
            }
        }

        private void NotifyBufferUpdated()
        {
            INotifyBufferUpdated tagger;
            if (_textBuffer.Properties.TryGetProperty<INotifyBufferUpdated>(typeof(PowerShellBraceMatchingTagger).Name, out tagger) && tagger != null)
            {
                tagger.OnBufferUpdated(_textBuffer);
            }
        }

        private void SetBufferProperty(object key, object propertyValue)
        {
            if (_textBuffer.Properties.ContainsProperty(key))
            {
                _textBuffer.Properties.RemoveProperty(key);
            }
            _textBuffer.Properties.AddProperty(key, propertyValue);
        }

        private void OnTokenizationComplete(Ast generatedAst)
        {
            if (TokenizationComplete != null)
            {
                TokenizationComplete(this, generatedAst);
            }
        }

        private void Tokenize(ITextSnapshot currentSnapshot,
                      string spanText,
                      int startPosition,
                      out Ast generatedAst,
                      out Token[] generatedTokens,
                      out List<ClassificationInfo> tokenSpans,
                      out List<TagInformation<ErrorTag>> errorTags,
                      out Dictionary<int, int> startBraces,
                      out Dictionary<int, int> endBraces,
                      out List<TagInformation<IOutliningRegionTag>> regions)
        {
            Log.Debug("Parsing input.");
            ParseError[] errors;
            generatedAst = Parser.ParseInput(spanText, out generatedTokens, out errors);

            Log.Debug("Classifying tokens.");
            tokenSpans = _classifierService.ClassifyTokens(generatedTokens, startPosition).ToList();

            Log.Debug("Tagging error spans.");
            // Trigger the out-proc error parsing only when there are errors from the in-proc parser
            if (errors.Length != 0)
            {
                var errorsParsedFromOutProc = PowerShellToolsPackage.IntelliSenseService.GetParseErrors(spanText);
                errorTags = _errorTagService.TagErrorSpans(currentSnapshot, startPosition, errorsParsedFromOutProc).ToList();
            }
            else
            {
                errorTags = _errorTagService.TagErrorSpans(currentSnapshot, startPosition, errors).ToList();
            }

            Log.Debug("Matching braces and regions.");
            _regionAndBraceMatchingService.GetRegionsAndBraceMatchingInformation(spanText, startPosition, generatedTokens, out startBraces, out endBraces, out regions);
        }

        private void SetTokenizationProperties(Ast generatedAst,
                              Token[] generatedTokens,
                              List<ClassificationInfo> tokenSpans,
                              List<TagInformation<ErrorTag>> errorTags,
                              Dictionary<int, int> startBraces,
                              Dictionary<int, int> endBraces,
                              List<TagInformation<IOutliningRegionTag>> regions)
        {
            SetBufferProperty(BufferProperties.Ast, generatedAst);
            SetBufferProperty(BufferProperties.Tokens, generatedTokens);
            SetBufferProperty(BufferProperties.TokenSpans, tokenSpans);
            SetBufferProperty(BufferProperties.TokenErrorTags, errorTags);
            SetBufferProperty(BufferProperties.StartBraces, startBraces);
            SetBufferProperty(BufferProperties.EndBraces, endBraces);
            SetBufferProperty(BufferProperties.Regions, regions);
        }

        private void RemoveCachedTokenizationProperties()
        {
            if (_textBuffer.Properties.ContainsProperty(BufferProperties.RegionTags))
            {
                _textBuffer.Properties.RemoveProperty(BufferProperties.RegionTags);
            }
        }
    }

    internal struct BraceInformation
    {
        internal char Character;
        internal int Position;

        internal BraceInformation(char character, int position)
        {
            Character = character;
            Position = position;
        }
    }

    internal struct ClassificationInfo
    {
        private readonly IClassificationType _classificationType;
        private readonly int _length;
        private readonly int _start;

        internal ClassificationInfo(int start, int length, IClassificationType classificationType)
        {
            _classificationType = classificationType;
            _start = start;
            _length = length;
        }

        internal int Length
        {
            get { return _length; }
        }

        internal int Start
        {
            get { return _start; }
        }

        internal IClassificationType ClassificationType
        {
            get { return _classificationType; }
        }
    }

    internal struct TagInformation<T> where T : ITag
    {
        internal readonly int Length;
        internal readonly int Start;
        internal readonly T Tag;

        internal TagInformation(int start, int length, T tag)
        {
            Tag = tag;
            Start = start;
            Length = length;
        }

        internal TagSpan<T> GetTagSpan(ITextSnapshot snapshot)
        {
            return snapshot.Length >= Start + Length ?
            new TagSpan<T>(new SnapshotSpan(snapshot, Start, Length), Tag) : null;
        }
    }

    public static class BufferProperties
    {
        public const string Ast = "PSAst";
        public const string Tokens = "PSTokens";
        public const string TokenErrorTags = "PSTokenErrorTags";
        public const string EndBraces = "PSEndBrace";
        public const string StartBraces = "PSStartBrace";
        public const string TokenSpans = "PSTokenSpans";
        public const string Regions = "PSRegions";
        public const string RegionTags = "PSRegionTags";
        public const string Classifier = "Classifier";
        public const string ErrorTagger = "PowerShellErrorTagger";
        public const string FromRepl = "PowerShellREPL";
        public const string LastWordReplacementSpan = "LastWordReplacementSpan";
        public const string LineUpToReplacementSpan = "LineUpToReplacementSpan";
        public const string SessionOriginIntellisense = "SessionOrigin_Intellisense";
        public const string SessionCompletionFullyMatchedStatus = "SessionCompletionFullyMatchedStatus";
        public const string PowerShellTokenizer = "PowerShellTokenizer";
    }

    public interface INotifyTagsChanged
    {
        void OnTagsChanged(SnapshotSpan span);
    }

    public interface INotifyBufferUpdated
    {
        void OnBufferUpdated(ITextBuffer textBuffer);
    }
}


