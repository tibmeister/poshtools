using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    public static class Classifications
    {
        public const string PowerShellAttribute = "PowerShellAttribute";
        public const string PowerShellCommand = "PowerShellCommand";
        public const string PowerShellCommandArgument = "PowerShellCommandArgument";
        public const string PowerShellCommandParameter = "PowerShellCommandParameter";
        public const string PowerShellComment = "PowerShellComment";
        public const string PowerShellKeyword = "PowerShellKeyword";
        public const string PowerShellNumber = "PowerShellNumber";
        public const string PowerShellOperator = "PowerShellOperator";
        public const string PowerShellString = "PowerShellString";
        public const string PowerShellType = "PowerShellType";
        public const string PowerShellVariable = "PowerShellVariable";
        public const string PowerShellMember = "PowerShellMember";
        public const string PowerShellGroupStart = "PowerShellGroupStart";
        public const string PowerShellGroupEnd = "PowerShellGroupEnd";
        public const string PowerShellLineCotinuation = "PowerShellLineCotinuation";
        public const string PowerShellLoopLabel = "PowerShellLoopLabel";
        public const string PowerShellNewLine = "PowerShellNewLine";
        public const string PowerShellPosition = "PowerShellPosition";
        public const string PowerShellStatementSeparator = "PowerShellStatementSeparator";
        public const string PowerShellUnknown = "PowerShellUnknown";
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellAttribute)]
    [Name("PowerShell Attribute")]
    [DisplayName("PowerShell Attribute")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellAttributeFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellCommand)]
    [Name("PowerShell Command")]
    [DisplayName("PowerShell Command")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellCommandFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellCommandArgument)]
    [Name("PowerShell Command Argument")]
    [DisplayName("PowerShell Command Argument")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellCommandArgumentFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellCommandParameter)]
    [Name("PowerShell Command Parameter")]
    [DisplayName("PowerShell Command Parameter")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellCommandParameterFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellComment)]
    [Name("PowerShell Comment")]
    [DisplayName("PowerShell Comment")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellCommentFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellKeyword)]
    [Name("PowerShell Keyword")]
    [DisplayName("PowerShell Keyword")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellKeywordFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellNumber)]
    [Name("PowerShell Number")]
    [DisplayName("PowerShell Number")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellNumberFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellOperator)]
    [Name("PowerShell Operator")]
    [DisplayName("PowerShell Operator")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellOperatorsFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellString)]
    [Name("PowerShell String")]
    [DisplayName("PowerShell String")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellStringFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellType)]
    [Name("PowerShell Type")]
    [DisplayName("PowerShell Types")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellTypeFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellVariable)]
    [Name("PowerShell Variable")]
    [DisplayName("PowerShell Variable")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellVariablesFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellMember)]
    [Name("PowerShell Member")]
    [DisplayName("PowerShell Member")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellMemberFormat : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellGroupStart)]
    [Name("PowerShell Group Start")]
    [DisplayName("PowerShell Group Start")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellGroupStartFormat: ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Classifications.PowerShellGroupEnd)]
    [Name("PowerShell Group End")]
    [DisplayName("PowerShell Group End")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class PowerShellGroupEndFormat : ClassificationFormatDefinition
    {
    }
}



