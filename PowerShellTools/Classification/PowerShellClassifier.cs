using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Management.Automation;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    internal class PowerShellClassifier : Classifier
    {
#pragma warning disable 169, 649
	[BaseDefinition(PredefinedClassificationTypeNames.SymbolDefinition), Name(Classifications.PowerShellAttribute), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition attributeTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.SymbolDefinition), Name(Classifications.PowerShellCommand), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition commandFormatTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Literal), Name(Classifications.PowerShellCommandArgument), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition commandArgumentTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellCommandParameter), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition commandParameterTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Comment), Name(Classifications.PowerShellComment), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition commentTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellGroupEnd), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition groupEndTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellGroupStart), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition groupStartTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Keyword), Name(Classifications.PowerShellKeyword), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition keywordTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellLineContinuation), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition lineContinuationTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellLoopLabel), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition loopLabelTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellMember), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition memberTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.WhiteSpace), Name(Classifications.PowerShellNewLine), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition newLineTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Number), Name(Classifications.PowerShellNumber), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition numberTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellOperator), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition operatorTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.WhiteSpace), Name(Classifications.PowerShellPosition), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition positionTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Operator), Name(Classifications.PowerShellStatementSeparator), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition statementSeparatorTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.String), Name(Classifications.PowerShellString), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition stringTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.SymbolDefinition), Name(Classifications.PowerShellType), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition typeTypeDefinition;
	[BaseDefinition(PredefinedClassificationTypeNames.Identifier), Name(Classifications.PowerShellVariable), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition variableTypeDefinition;
	[BaseDefinition("text"), Name("PowerShell TokenInBreakpoint"), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition tokenInBreakpoinTypeDefinition;
	[BaseDefinition("text"), Name("PS1ScriptGaps"), Export(typeof(ClassificationTypeDefinition))]
	private static ClassificationTypeDefinition scriptGapsTypeDefinition;
	private static Dictionary<PSTokenType, IClassificationType> tokenClassificationTypeMap;
	private static IClassificationType scriptGaps;
#pragma warning restore 169, 649

	static PowerShellClassifier()
	{
	    tokenClassificationTypeMap = new Dictionary<PSTokenType, IClassificationType>();
	    tokenClassificationTypeMap[PSTokenType.Attribute] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellAttribute);
	    tokenClassificationTypeMap[PSTokenType.Command] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellCommand);
	    tokenClassificationTypeMap[PSTokenType.CommandArgument] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellCommandArgument);
	    tokenClassificationTypeMap[PSTokenType.CommandParameter] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellCommandParameter);
	    tokenClassificationTypeMap[PSTokenType.Comment] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellComment);
	    tokenClassificationTypeMap[PSTokenType.GroupEnd] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellGroupEnd);
	    tokenClassificationTypeMap[PSTokenType.GroupStart] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellGroupStart);
	    tokenClassificationTypeMap[PSTokenType.Keyword] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellKeyword);
	    tokenClassificationTypeMap[PSTokenType.LineContinuation] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellLineContinuation);
	    tokenClassificationTypeMap[PSTokenType.LoopLabel] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellLoopLabel);
	    tokenClassificationTypeMap[PSTokenType.Member] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellMember);
	    tokenClassificationTypeMap[PSTokenType.NewLine] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellNewLine);
	    tokenClassificationTypeMap[PSTokenType.Number] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellNumber);
	    tokenClassificationTypeMap[PSTokenType.Operator] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellOperator);
	    tokenClassificationTypeMap[PSTokenType.Position] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellPosition);
	    tokenClassificationTypeMap[PSTokenType.StatementSeparator] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellStatementSeparator);
	    tokenClassificationTypeMap[PSTokenType.String] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellString);
	    tokenClassificationTypeMap[PSTokenType.Type] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellType);
	    tokenClassificationTypeMap[PSTokenType.Unknown] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellUnknown);
	    tokenClassificationTypeMap[PSTokenType.Variable] = EditorImports.ClassificationTypeRegistryService.GetClassificationType(Classifications.PowerShellVariable);
	    scriptGaps = EditorImports.ClassificationTypeRegistryService.GetClassificationType("PS1ScriptGaps");
	}

	internal PowerShellClassifier(ITextBuffer bufferToClassify)
	    : base(bufferToClassify)
	{

	}

	protected override IList<ClassificationSpan> VirtualGetClassificationSpans(SnapshotSpan span)
	{
	    var list = new List<ClassificationSpan>();
	    if (span.Snapshot == null || span.Snapshot.Length == 0)
	    {
		return list;
	    }

	    AddTokenClassifications(TextBuffer, span, list, null, scriptGaps);
	    FillBeginningAndEnd(span, list, TextBuffer.CurrentSnapshot, scriptGaps);
	    return list;
	}

	internal static IClassificationType GetClassificationType(PSTokenType tokenType)
	{
	    IClassificationType result;
	    return tokenClassificationTypeMap.TryGetValue(tokenType, out result) ? result : null;
	}

	private void FillClassificationGap(List<ClassificationSpan> classifications, Span? lastClassificationSpan, Span newClassificationSpan, ITextSnapshot currentSnapshot, IClassificationType classificationType)
	{
	    if (lastClassificationSpan.HasValue && newClassificationSpan.Start > lastClassificationSpan.Value.Start + lastClassificationSpan.Value.Length)
	    {
		classifications.Add(new ClassificationSpan(new SnapshotSpan(currentSnapshot, lastClassificationSpan.Value.Start + lastClassificationSpan.Value.Length, newClassificationSpan.Start - (lastClassificationSpan.Value.Start + lastClassificationSpan.Value.Length)), classificationType));
	    }
	}

	private void FillBeginningAndEnd(SnapshotSpan span, List<ClassificationSpan> classifications, ITextSnapshot currentSnapshot, IClassificationType classificationType)
	{
	    if (classifications.Count == 0)
	    {
		classifications.Add(new ClassificationSpan(new SnapshotSpan(currentSnapshot, span.Start, span.Length), classificationType));
		return;
	    }

	    var classificationSpan = classifications[0];
	    if (span.Start < classificationSpan.Span.Start)
	    {
		var item = new ClassificationSpan(new SnapshotSpan(currentSnapshot, span.Start, classificationSpan.Span.Start - span.Start), classificationType);
		classifications.Insert(0, item);
	    }

	    var classificationSpan2 = classifications[classifications.Count - 1];
	    if (classificationSpan2.Span.End < span.End)
	    {
		classifications.Add(new ClassificationSpan(new SnapshotSpan(currentSnapshot, classificationSpan2.Span.End, span.End - classificationSpan2.Span.End), classificationType));
	    }
	}

	private void AddTokenClassifications(ITextBuffer buffer, SnapshotSpan span, List<ClassificationSpan> classifications, Span? lastClassificationSpan, IClassificationType gapType)
	{
	    List<ClassificationInfo> spans = null;
	    if (!buffer.Properties.TryGetProperty<List<ClassificationInfo>>(BufferProperties.TokenSpans, out spans) || spans == null)
	    {
		return;
	    }

	    foreach (var current in spans)
	    {
		if (current.Start + current.Length < span.Start) continue;

		if (current.Start > span.End)
		{
		    break;
		}

		if (current.Start + current.Length > buffer.CurrentSnapshot.Length) continue;

		var snapshotSpan = new SnapshotSpan(span.Snapshot, current.Start, current.Length);
		var classificationSpan = new ClassificationSpan(snapshotSpan, current.ClassificationType);
		FillClassificationGap(classifications, lastClassificationSpan, snapshotSpan, buffer.CurrentSnapshot, gapType);
		lastClassificationSpan = snapshotSpan;
		classifications.Add(classificationSpan);
	    }
	}
    }
}
