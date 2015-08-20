using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using log4net;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Tasks = System.Threading.Tasks;

namespace PowerShellTools.Classification
{
    internal class PowerShellTokenizationService : IPowerShellTokenizationService
    {
	private static readonly ILog Log = LogManager.GetLogger(typeof(PowerShellTokenizationService));
	private readonly object _tokenizationLock = new object();

	public event EventHandler<Ast> TokenizationComplete;

	private readonly ClassifierService _classifierService;
	private readonly ErrorTagSpanService _errorTagService;
	private readonly RegionAndBraceMatchingService _regionAndBraceMatchingService;

	private ITextBuffer _textBuffer;
	private ITextSnapshot _lastSnapshot;
	private bool _isBufferTokenizing;

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


