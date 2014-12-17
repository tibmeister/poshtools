using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading;
using log4net;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace PowerShellTools.Classification
{
	internal class PowerShellTokenizationService
	{
	    private static readonly ILog Log = LogManager.GetLogger(typeof (PowerShellTokenizationService));

	    private bool _isBufferTokenizing;

	    private static readonly Token[] EmptyTokens = new Token[0];
	    private static readonly Dictionary<int, int> EmptyDictionary = new Dictionary<int, int>();

	    private static readonly List<TagInformation<IOutliningRegionTag>> EmptyRegions =
            new List<TagInformation<IOutliningRegionTag>>();

	    private static readonly List<ClassificationInfo> EmptyTokenSpans = new List<ClassificationInfo>();
	    private static readonly List<TagInformation<ErrorTag>> EmptyErrorTags = new List<TagInformation<ErrorTag>>();

	    private Dictionary<int, int> _endBraces;
	    private IEnumerable<TagInformation<ErrorTag>> _errorTags;
	    private Ast _generatedAst;
	    private Token[] _generatedTokens;
	    private List<TagInformation<IOutliningRegionTag>> _regions;
	    private Dictionary<int, int> _startBraces;
        private IEnumerable<ClassificationInfo> _tokenSpans;
	    internal event EventHandler<EventArgs> TokenizationComplete;

	    internal ITrackingSpan SpanToTokenize { get; set; }
	    private ITextBuffer Buffer { get; set; }

	    private readonly ClassifierService _classifierService;
	    private readonly ErrorTagSpanService _errorTagService;
	    private readonly RegionAndBraceMatchingService _regionAndBraceMatchingService;

	    public PowerShellTokenizationService(ITextBuffer buffer)
		{
			Buffer = buffer;
            _classifierService = new ClassifierService();
            _errorTagService = new ErrorTagSpanService();
            _regionAndBraceMatchingService = new RegionAndBraceMatchingService();

            SetEmptyTokenizationProperties();
            SpanToTokenize = Buffer.CurrentSnapshot.CreateTrackingSpan(0, Buffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeInclusive);
            StartTokenization();
		}

	    internal void StartTokenization()
	    {
	        var spanToTokenizeCache = SpanToTokenize;

	        if (spanToTokenizeCache == null)
	        {
	            return;
	        }
	        if (_isBufferTokenizing)
	        {
                Log.Debug("Already tokenizing...");
	            return;
	        }

	        _isBufferTokenizing = true;
	        SetEmptyTokenizationProperties();
	        if (Buffer.CurrentSnapshot.Length == 0)
	        {
	            RemoveCachedTokenizationProperties();
	            OnTokenizationComplete();
	            _isBufferTokenizing = false;
	            return;
	        }

	        var tokenizationText = spanToTokenizeCache.GetText(Buffer.CurrentSnapshot);

	        ThreadPool.QueueUserWorkItem(delegate
	        {
	            var done = false;
	            while (!done)
	            {
	                Tokenize(spanToTokenizeCache, tokenizationText);
	                var trackingSpan = SpanToTokenize;
	                if (!ReferenceEquals(trackingSpan, spanToTokenizeCache))
	                {
	                    spanToTokenizeCache = trackingSpan;
	                    tokenizationText = spanToTokenizeCache.GetText(Buffer.CurrentSnapshot);
	                    return;
	                }
	                SetTokenizationProperties();
	                RemoveCachedTokenizationProperties();
	                SetBufferProperty(BufferProperties.SpanTokenized, spanToTokenizeCache);
	                OnTokenizationComplete();
	                _isBufferTokenizing = false;
	                done = true;

	                NotifyOnTagsChanged(BufferProperties.Classifier);
                    NotifyOnTagsChanged(BufferProperties.ErrorTagger);
                    NotifyOnTagsChanged(typeof(PowerShellOutliningTagger).Name);
	            }
	        }, this);
	    }

	    private void NotifyOnTagsChanged(string name)
	    {
	        if (!Buffer.Properties.ContainsProperty(name)) return;
	        var classifier = Buffer.Properties.GetProperty<INotifyTagsChanged>(name);
	        if (classifier != null)
	        {
	            classifier.OnTagsChanged(SpanToTokenize.GetSpan(Buffer.CurrentSnapshot));
	        }
	    }

	    private void SetBufferProperty(object key, object propertyValue)
		{
			if (Buffer.Properties.ContainsProperty(key))
			{
				Buffer.Properties.RemoveProperty(key);
			}
			Buffer.Properties.AddProperty(key, propertyValue);
		}

		private void OnTokenizationComplete()
		{
            if (TokenizationComplete != null)
			{
                TokenizationComplete(this, new EventArgs());
			}
		}

	    internal void SetEmptyTokenizationProperties()
	    {
	        SetBufferProperty(BufferProperties.Tokens, EmptyTokens);
	        SetBufferProperty(BufferProperties.Ast, null);
            SetBufferProperty(BufferProperties.TokenErrorTags, EmptyErrorTags);
            SetBufferProperty(BufferProperties.EndBrace, EmptyDictionary);
            SetBufferProperty(BufferProperties.StartBrace, EmptyDictionary);
            SetBufferProperty(BufferProperties.TokenSpans, EmptyTokenSpans);
            SetBufferProperty(BufferProperties.SpanTokenized, Buffer.CurrentSnapshot.CreateTrackingSpan(0, 0, SpanTrackingMode.EdgeInclusive));
            SetBufferProperty(BufferProperties.Regions, EmptyRegions);
	    }

	    private void Tokenize(ITrackingSpan spanToTokenize, string spanText)
	    {
            Log.Debug("Parsing input.");
	        ParseError[] errors;
	        _generatedAst = Parser.ParseInput(spanText, out _generatedTokens, out errors);
	        
	        var position = spanToTokenize.GetStartPoint(Buffer.CurrentSnapshot).Position;
	        var array = _generatedTokens;

            Log.Debug("Classifying tokens.");
            _tokenSpans = _classifierService.ClassifyTokens(array, position);

            Log.Debug("Tagging error spans.");
	        _errorTags = _errorTagService.TagErrorSpans(Buffer, position, errors).ToList();

            Log.Debug("Matching braces and regions.");
            _regionAndBraceMatchingService.GetRegionsAndBraceMatchingInformation(spanText, position, _generatedTokens, out _startBraces, out _endBraces, out _regions);
	    }

	    private void SetTokenizationProperties()
	    {
	        SetBufferProperty(BufferProperties.Tokens, _generatedTokens);
            SetBufferProperty(BufferProperties.Ast, _generatedAst);
	        SetBufferProperty(BufferProperties.SpanTokenized, null);
	        SetBufferProperty(BufferProperties.TokenErrorTags, _errorTags);
	        SetBufferProperty(BufferProperties.EndBrace, _endBraces);
	        SetBufferProperty(BufferProperties.StartBrace, _startBraces);
	        SetBufferProperty(BufferProperties.Regions, _regions);
	        SetBufferProperty(BufferProperties.TokenSpans, _tokenSpans);
	    }

	    private void RemoveCachedTokenizationProperties()
	    {
	        Buffer.Properties.RemoveProperty(BufferProperties.RegionTags);
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
            return new TagSpan<T>(new SnapshotSpan(snapshot, Start, Length), Tag);
        }
    }

    public static class BufferProperties
    {
        public const string Ast = "PSAst";
        public const string Tokens = "PSTokens";
        public const string TokenErrorTags = "PSTokenErrorTags";
        public const string EndBrace = "PSEndBrace";
        public const string StartBrace = "PSStartBrace";
        public const string TokenSpans = "PSTokenSpans";
        public const string SpanTokenized = "PSSpanTokenized";
        public const string Regions = "PSRegions";
        public const string RegionTags = "PSRegionTags";
        public const string Classifier = "PowerShellClassifier";
        public const string ErrorTagger = "PowerShellErrorTagger";
        public const string FromRepl = "REPL";
        public const string LastWordReplacementSpan = "LastWordReplacementSpan";
        public const string LineUpToReplacementSpan = "LineUpToReplacementSpan";
        public const string SessionOriginIntellisense = "SessionOrigin_Intellisense";
    }

    public interface INotifyTagsChanged
    {
        void OnTagsChanged(SnapshotSpan span);
    }
}


