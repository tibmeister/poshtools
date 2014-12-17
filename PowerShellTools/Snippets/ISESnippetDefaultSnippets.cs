using Microsoft.PowerShell.Host.ISE;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace Microsoft.Windows.PowerShell.Gui.Internal
{
	internal static class ISESnippetDefaultSnippets
	{
		private const string CodeForIfSnippet = "if ($x -gt $y)\r\n{\r\n    \r\n}";
		private const string CodeForIfElseSnippet = "if ($x -lt $y)\r\n{\r\n    \r\n}\r\nelse\r\n{\r\n    \r\n}";
		private const string CodeForForLoopSnippet = "for ($i = 1; $i -lt 99; $i++)\r\n{ \r\n    \r\n}";
		private const string CodeForForEachSnippet = "foreach ($item in $collection)\r\n{\r\n    \r\n}";
		private const string CodeForFunction2ParamSnippet = "function MyFunction ($param1, $param2)\r\n{\r\n    \r\n}";
		private const string CodeForFunctionAdvancedSnippet = "<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExample>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExample>!>\r\n#>\r\nfunction Verb-Noun\r\n{\r\n    [CmdletBinding()]\r\n    [OutputType([int])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true,\r\n                   ValueFromPipelineByPropertyName=$true,\r\n                   Position=0)]\r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n    Begin\r\n    {\r\n    }\r\n    Process\r\n    {\r\n    }\r\n    End\r\n    {\r\n    }\r\n}";
		private const string CodeForFunctionAdvancedBigSnippet = "<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExample>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExample>!>\r\n.INPUTS\r\n   <!<SnippetInput>!>\r\n.OUTPUTS\r\n   <!<SnippetOutput>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.COMPONENT\r\n   <!<SnippetComponent>!>\r\n.ROLE\r\n   <!<SnippetRole>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetFunctionality>!>\r\n#>\r\nfunction Verb-Noun\r\n{\r\n    [CmdletBinding(DefaultParameterSetName='Parameter Set 1', \r\n                  SupportsShouldProcess=$true, \r\n                  PositionalBinding=$false,\r\n                  HelpUri = 'http://www.microsoft.com/',\r\n                  ConfirmImpact='Medium')]\r\n    [OutputType([String])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true, \r\n                   ValueFromPipeline=$true,\r\n                   ValueFromPipelineByPropertyName=$true, \r\n                   ValueFromRemainingArguments=$false, \r\n                   Position=0,\r\n                   ParameterSetName='Parameter Set 1')]\r\n        [ValidateNotNull()]\r\n        [ValidateNotNullOrEmpty()]\r\n        [ValidateCount(0,5)]\r\n        [ValidateSet(\"sun\", \"moon\", \"earth\")]\r\n        [Alias(\"p1\")] \r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [Parameter(ParameterSetName='Parameter Set 1')]\r\n        [AllowNull()]\r\n        [AllowEmptyCollection()]\r\n        [AllowEmptyString()]\r\n        [ValidateScript({$true})]\r\n        [ValidateRange(0,5)]\r\n        [int]\r\n        $Param2,\r\n\r\n        # <!<SnippetParam3Help>!>\r\n        [Parameter(ParameterSetName='Another Parameter Set')]\r\n        [ValidatePattern(\"[a-z]*\")]\r\n        [ValidateLength(0,15)]\r\n        [String]\r\n        $Param3\r\n    )\r\n\r\n    Begin\r\n    {\r\n    }\r\n    Process\r\n    {\r\n        if ($pscmdlet.ShouldProcess(\"Target\", \"Operation\"))\r\n        {\r\n        }\r\n    }\r\n    End\r\n    {\r\n    }\r\n}";
		private const string CodeForSwitchSnippet = "switch ($x)\r\n{\r\n    'value1' {}\r\n    {$_ -in 'A','B','C'} {}\r\n    'value3' {}\r\n    Default {}\r\n}";
		private const string CodeForWhileSnippet = "while ($x -gt 0)\r\n{\r\n    \r\n}";
		private const string CodeForDoWhileSnippet = "do\r\n{\r\n    \r\n}\r\nwhile ($x -gt 0)";
		private const string CodeForDoUntilSnippet = "do\r\n{\r\n    \r\n}\r\nuntil ($x -gt 0)";
		private const string CodeForTryCatchFinallySnippet = "try\r\n{\r\n    1/0\r\n}\r\ncatch [DivideByZeroException]\r\n{\r\n    Write-Host \"<!<SnippetDivideByZeroException>!>\"\r\n}\r\ncatch [System.Net.WebException],[System.Exception]\r\n{\r\n    Write-Host \"<!<SnippetOtherException>!>\"\r\n}\r\nfinally\r\n{\r\n    Write-Host \"<!<SnippetCleaningUp>!>\"\r\n}";
		private const string CodeForTryFinallySnippet = "try\r\n{\r\n    \r\n}\r\nfinally\r\n{\r\n    \r\n}";
		private const string CommentBlockSnippet = "<#\r\n # \r\n#>\r\n";
		private const string WorkflowInlineScriptSnippetCode = "inlineScript\r\n{\r\n\r\n} # <!<SnippetOptionalWorkflow>!>";
		private const string WorkflowParallelSnippetCode = "parallel\r\n{\r\n\r\n}";
		private const string WorkflowSequenceSnippetCode = "sequence\r\n{\r\n\r\n}";
		private const string WorkflowForEachParallelSnippetCode = "foreach -parallel ($item in $collection)\r\n{\r\n\r\n}";
		private const string WorkflowSimpleCode = "<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExampleWorkflow>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExampleWorkflow>!>\r\n.INPUTS\r\n   <!<SnippetInputWorkflow>!>\r\n.OUTPUTS\r\n   <!<SnippetOutputWorkflow>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetsFunctionalityWorkflow>!>\r\n#>\r\nworkflow Verb-Noun \r\n{\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [string]\r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n}";
		private const string WorkflowAdvancedCode = "workflow Verb-Noun\r\n{\r\n<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExampleWorkflow>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExampleWorkflow>!>\r\n.INPUTS\r\n   <!<SnippetInputWorkflow>!>\r\n.OUTPUTS\r\n   <!<SnippetOutputWorkflow>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetsFunctionalityWorkflow>!>\r\n#>\r\n\r\n    [CmdletBinding(DefaultParameterSetName='Parameter Set 1',\r\n                  HelpUri = 'http://www.microsoft.com/',\r\n                  ConfirmImpact='Medium')]\r\n    [OutputType([String])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true, \r\n                   Position=0,\r\n                   ParameterSetName='Parameter Set 1')]\r\n        [ValidateNotNull()]\r\n        [Alias(\"p1\")] \r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n    # <!<SnippetCheckpointCommentWorkflow>!>\r\n    # Checkpoint-Workflow\r\n\r\n    # <!<SnippetSuspendCommentWorkflow>!>\r\n    # Suspend-Workflow \r\n\r\n    # <!<SnippetsCommonParamterWorkflow>!>\r\n    $PSPersist \r\n    $PSComputerName\r\n    $PSCredential\r\n    $PSUseSsl\r\n    $PSAuthentication\r\n\r\n    # <!<SnippetsRuntimeWorkflow>!>\r\n    $Input\r\n    $PSSenderInfo\r\n    $PSWorkflowRoot\r\n    $JobCommandName\r\n    $ParentCommandName\r\n    $JobId\r\n    $ParentJobId\r\n    $WorkflowInstanceId\r\n    $JobInstanceId\r\n    $ParentJobInstanceId\r\n    $JobName\r\n    $ParentJobName\r\n\r\n    # <!<SnippetsParentActivityWorkflow>!>\r\n    $PSParentActivityId\r\n\r\n    # <!<SnipetsPreferenceWorkflow>!>\r\n    $PSRunInProcessPreference\r\n    $PSPersistPreference\r\n}";
		private const string CodeForDSCResourceProviderSnippet = "Function Get-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}\r\n\r\nFunction Set-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}\r\n\r\nFunction Test-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}";
		private const string CodeForDSCConfigurationSnippet = "configuration Name\r\n{\r\n    # <!<SnippetDSCConfigurationComment1>!>\r\n    # <!<SnippetDSCConfigurationComment2>!>\r\n    node (\"Node1\",\"Node2\",\"Node3\")\r\n    {\r\n        # <!<SnippetDSCConfigurationComment3>!>\r\n        # <!<SnippetDSCConfigurationComment4>!>\r\n        WindowsFeature FriendlyName\r\n        {\r\n           Ensure = \"Present\"\r\n           Name = \"Feature Name\"\r\n        }\r\n\r\n        File FriendlyName\r\n        {\r\n            Ensure = \"Present\"\r\n            SourcePath = $SourcePath\r\n            DestinationPath = $DestinationPath\r\n            Type = \"Directory\"\r\n            Requires = \"[WindowsFeature]FriendlyName\"\r\n        }       \r\n    }\r\n}";
		private static readonly ISESnippet defaultSnippetForIf = new ISESnippet("if", new Version("1.0.0"), Strings.Format(GuiStrings.DefaultSnippetDescriptionStatement, new object[]
		{
			"if"
		}), GuiStrings.DefaultSnippetAuthor, "if ($x -gt $y)\r\n{\r\n    \r\n}", 5);
		private static readonly ISESnippet defaultSnippetForIfElse = new ISESnippet("if-else", new Version("1.0.0"), Strings.Format(GuiStrings.DefaultSnippetDescriptionStatement, new object[]
		{
			"if-else"
		}), GuiStrings.DefaultSnippetAuthor, "if ($x -lt $y)\r\n{\r\n    \r\n}\r\nelse\r\n{\r\n    \r\n}", 5);
		private static readonly ISESnippet defaultSnippetForForLoop = new ISESnippet("for", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFor, new object[]
		{
			"for"
		}), GuiStrings.DefaultSnippetAuthor, "for ($i = 1; $i -lt 99; $i++)\r\n{ \r\n    \r\n}", 6);
		private static readonly ISESnippet defaultSnippetForForeach = new ISESnippet("foreach", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFor, new object[]
		{
			"foreach"
		}), GuiStrings.DefaultSnippetAuthor, "foreach ($item in $collection)\r\n{\r\n    \r\n}", 10);
		private static readonly ISESnippet defaultSnippetForFunction2Param = new ISESnippet("function", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFunction2Param, new object[]
		{
			"function"
		}), GuiStrings.DefaultSnippetAuthor, "function MyFunction ($param1, $param2)\r\n{\r\n    \r\n}", 9);
		private static readonly ISESnippet defaultSnippetForFunctionAdvanced = new ISESnippet(string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetCmdletAdvancedFunction, new object[]
		{
			"Cmdlet"
		}), new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFunctionAdvanced, new object[]
		{
			"function",
			"ScriptCmdlet",
			"attributes"
		}), GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExample>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExample>!>\r\n#>\r\nfunction Verb-Noun\r\n{\r\n    [CmdletBinding()]\r\n    [OutputType([int])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true,\r\n                   ValueFromPipelineByPropertyName=$true,\r\n                   Position=0)]\r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n    Begin\r\n    {\r\n    }\r\n    Process\r\n    {\r\n    }\r\n    End\r\n    {\r\n    }\r\n}"), 0);
		private static readonly ISESnippet defaultSnippetForFunctionAdvancedBig = new ISESnippet(string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetCmdletAdvancedFunctionComplete, new object[]
		{
			"Cmdlet"
		}), new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFunctionAdvancedBig, new object[]
		{
			"function",
			"ScriptCmdlet",
			"attributes"
		}), GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExample>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExample>!>\r\n.INPUTS\r\n   <!<SnippetInput>!>\r\n.OUTPUTS\r\n   <!<SnippetOutput>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.COMPONENT\r\n   <!<SnippetComponent>!>\r\n.ROLE\r\n   <!<SnippetRole>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetFunctionality>!>\r\n#>\r\nfunction Verb-Noun\r\n{\r\n    [CmdletBinding(DefaultParameterSetName='Parameter Set 1', \r\n                  SupportsShouldProcess=$true, \r\n                  PositionalBinding=$false,\r\n                  HelpUri = 'http://www.microsoft.com/',\r\n                  ConfirmImpact='Medium')]\r\n    [OutputType([String])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true, \r\n                   ValueFromPipeline=$true,\r\n                   ValueFromPipelineByPropertyName=$true, \r\n                   ValueFromRemainingArguments=$false, \r\n                   Position=0,\r\n                   ParameterSetName='Parameter Set 1')]\r\n        [ValidateNotNull()]\r\n        [ValidateNotNullOrEmpty()]\r\n        [ValidateCount(0,5)]\r\n        [ValidateSet(\"sun\", \"moon\", \"earth\")]\r\n        [Alias(\"p1\")] \r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [Parameter(ParameterSetName='Parameter Set 1')]\r\n        [AllowNull()]\r\n        [AllowEmptyCollection()]\r\n        [AllowEmptyString()]\r\n        [ValidateScript({$true})]\r\n        [ValidateRange(0,5)]\r\n        [int]\r\n        $Param2,\r\n\r\n        # <!<SnippetParam3Help>!>\r\n        [Parameter(ParameterSetName='Another Parameter Set')]\r\n        [ValidatePattern(\"[a-z]*\")]\r\n        [ValidateLength(0,15)]\r\n        [String]\r\n        $Param3\r\n    )\r\n\r\n    Begin\r\n    {\r\n    }\r\n    Process\r\n    {\r\n        if ($pscmdlet.ShouldProcess(\"Target\", \"Operation\"))\r\n        {\r\n        }\r\n    }\r\n    End\r\n    {\r\n    }\r\n}"), 0);
		private static readonly ISESnippet defaultSnippetForSwitch = new ISESnippet("switch", new Version("1.0.0"), Strings.Format(GuiStrings.DefaultSnippetDescriptionStatement, new object[]
		{
			"switch"
		}), GuiStrings.DefaultSnippetAuthor, "switch ($x)\r\n{\r\n    'value1' {}\r\n    {$_ -in 'A','B','C'} {}\r\n    'value3' {}\r\n    Default {}\r\n}", 9);
		private static readonly ISESnippet defaultSnippetForWhile = new ISESnippet("while", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFor, new object[]
		{
			"while"
		}), GuiStrings.DefaultSnippetAuthor, "while ($x -gt 0)\r\n{\r\n    \r\n}", 8);
		private static readonly ISESnippet defaultSnippetForDoWhile = new ISESnippet("do-while", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFor, new object[]
		{
			"do-while"
		}), GuiStrings.DefaultSnippetAuthor, "do\r\n{\r\n    \r\n}\r\nwhile ($x -gt 0)", 24);
		private static readonly ISESnippet defaultSnippetForDoUntil = new ISESnippet("do-until", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionFor, new object[]
		{
			"do-until"
		}), GuiStrings.DefaultSnippetAuthor, "do\r\n{\r\n    \r\n}\r\nuntil ($x -gt 0)", 24);
		private static readonly ISESnippet defaultSnippetForTryCatchFinally = new ISESnippet("try-catch-finally", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetExceptionHandling, new object[]
		{
			"try-catch-finally"
		}), GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("try\r\n{\r\n    1/0\r\n}\r\ncatch [DivideByZeroException]\r\n{\r\n    Write-Host \"<!<SnippetDivideByZeroException>!>\"\r\n}\r\ncatch [System.Net.WebException],[System.Exception]\r\n{\r\n    Write-Host \"<!<SnippetOtherException>!>\"\r\n}\r\nfinally\r\n{\r\n    Write-Host \"<!<SnippetCleaningUp>!>\"\r\n}"), 12);
		private static readonly ISESnippet defaultSnippetForTryFinally = new ISESnippet("try-finally", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetExceptionHandling, new object[]
		{
			"try-finally"
		}), GuiStrings.DefaultSnippetAuthor, "try\r\n{\r\n    \r\n}\r\nfinally\r\n{\r\n    \r\n}", 12);
		private static readonly ISESnippet defaultSnippetForComment = new ISESnippet(GuiStrings.DefaultSnippetCommentBlock, new Version("1.0.0"), GuiStrings.DefaultSnippetCommentBlock, GuiStrings.DefaultSnippetAuthor, "<#\r\n # \r\n#>\r\n", 7);
		private static readonly ISESnippet workflowInlineScriptSnippet = new ISESnippet("Workflow InlineScript", new Version("1.0.0"), GuiStrings.WorkflowInlineScriptSnippetDescription, GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("inlineScript\r\n{\r\n\r\n} # <!<SnippetOptionalWorkflow>!>"), 17);
		private static readonly ISESnippet workflowParallelSnippet = new ISESnippet("Workflow Parallel", new Version("1.0.0"), GuiStrings.WorkflowParallelSnippetDescription, GuiStrings.DefaultSnippetAuthor, "parallel\r\n{\r\n\r\n}", 13);
		private static readonly ISESnippet workflowSequenceSnippet = new ISESnippet("Workflow Sequence", new Version("1.0.0"), GuiStrings.WorkflowSequenceSnippetDescription, GuiStrings.DefaultSnippetAuthor, "sequence\r\n{\r\n\r\n}", 13);
		private static readonly ISESnippet workflowForEachParallelSnippet = new ISESnippet("Workflow ForEachParallel", new Version("1.0.0"), Strings.Format(GuiStrings.WorkflowSequenceForEachParallelDescriptionFormat, new object[]
		{
			"foreach"
		}), GuiStrings.DefaultSnippetAuthor, "foreach -parallel ($item in $collection)\r\n{\r\n\r\n}", 45);
		private static readonly ISESnippet workflowSimpleCodeSnippet = new ISESnippet("Workflow (simple)", new Version("1.0.0"), GuiStrings.WorkflowSimpleDescription, GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExampleWorkflow>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExampleWorkflow>!>\r\n.INPUTS\r\n   <!<SnippetInputWorkflow>!>\r\n.OUTPUTS\r\n   <!<SnippetOutputWorkflow>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetsFunctionalityWorkflow>!>\r\n#>\r\nworkflow Verb-Noun \r\n{\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [string]\r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n}"), 0);
		private static readonly ISESnippet workflowAdvancedCodeSnippet = new ISESnippet("Workflow (advanced)", new Version("1.0.0"), GuiStrings.WorkflowAdvancedDescription, GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("workflow Verb-Noun\r\n{\r\n<#\r\n.Synopsis\r\n   <!<SnippetShortDescription>!>\r\n.DESCRIPTION\r\n   <!<SnippetLongDescription>!>\r\n.EXAMPLE\r\n   <!<SnippetExampleWorkflow>!>\r\n.EXAMPLE\r\n   <!<SnippetAnotherExampleWorkflow>!>\r\n.INPUTS\r\n   <!<SnippetInputWorkflow>!>\r\n.OUTPUTS\r\n   <!<SnippetOutputWorkflow>!>\r\n.NOTES\r\n   <!<SnippetNotes>!>\r\n.FUNCTIONALITY\r\n   <!<SnippetsFunctionalityWorkflow>!>\r\n#>\r\n\r\n    [CmdletBinding(DefaultParameterSetName='Parameter Set 1',\r\n                  HelpUri = 'http://www.microsoft.com/',\r\n                  ConfirmImpact='Medium')]\r\n    [OutputType([String])]\r\n    Param\r\n    (\r\n        # <!<SnippetParam1Help>!>\r\n        [Parameter(Mandatory=$true, \r\n                   Position=0,\r\n                   ParameterSetName='Parameter Set 1')]\r\n        [ValidateNotNull()]\r\n        [Alias(\"p1\")] \r\n        $Param1,\r\n\r\n        # <!<SnippetParam2Help>!>\r\n        [int]\r\n        $Param2\r\n    )\r\n\r\n    # <!<SnippetCheckpointCommentWorkflow>!>\r\n    # Checkpoint-Workflow\r\n\r\n    # <!<SnippetSuspendCommentWorkflow>!>\r\n    # Suspend-Workflow \r\n\r\n    # <!<SnippetsCommonParamterWorkflow>!>\r\n    $PSPersist \r\n    $PSComputerName\r\n    $PSCredential\r\n    $PSUseSsl\r\n    $PSAuthentication\r\n\r\n    # <!<SnippetsRuntimeWorkflow>!>\r\n    $Input\r\n    $PSSenderInfo\r\n    $PSWorkflowRoot\r\n    $JobCommandName\r\n    $ParentCommandName\r\n    $JobId\r\n    $ParentJobId\r\n    $WorkflowInstanceId\r\n    $JobInstanceId\r\n    $ParentJobInstanceId\r\n    $JobName\r\n    $ParentJobName\r\n\r\n    # <!<SnippetsParentActivityWorkflow>!>\r\n    $PSParentActivityId\r\n\r\n    # <!<SnipetsPreferenceWorkflow>!>\r\n    $PSRunInProcessPreference\r\n    $PSPersistPreference\r\n}"), 0);
		private static readonly ISESnippet defaultSnippetForDSCResourceProvider = new ISESnippet("DSC Resource Provider (simple)", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionDSCResourceProvider, new object[]
		{
			"Get-TargetResource",
			"Set-TargetResource",
			"Test-TargetResource"
		}), GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("Function Get-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}\r\n\r\nFunction Set-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}\r\n\r\nFunction Test-TargetResource\r\n{\r\n    # <!<SnippetDSCReourceProviderComment1>!>\r\n    # <!<SnippetDSCReourceProviderComment2>!>\r\n    # <!<SnippetDSCReourceProviderComment3>!>\r\n    param(\r\n    )\r\n}"), 0);
		private static readonly ISESnippet defaultSnippetForDSCConfiguration = new ISESnippet("DSC Configuration (simple)", new Version("1.0.0"), string.Format(CultureInfo.CurrentUICulture, GuiStrings.DefaultSnippetDescriptionDSCConfiguration, new object[]
		{
			"WindowsFeature",
			"File"
		}), GuiStrings.DefaultSnippetAuthor, ISESnippetDefaultSnippets.ReplaceResourceMarkers("configuration Name\r\n{\r\n    # <!<SnippetDSCConfigurationComment1>!>\r\n    # <!<SnippetDSCConfigurationComment2>!>\r\n    node (\"Node1\",\"Node2\",\"Node3\")\r\n    {\r\n        # <!<SnippetDSCConfigurationComment3>!>\r\n        # <!<SnippetDSCConfigurationComment4>!>\r\n        WindowsFeature FriendlyName\r\n        {\r\n           Ensure = \"Present\"\r\n           Name = \"Feature Name\"\r\n        }\r\n\r\n        File FriendlyName\r\n        {\r\n            Ensure = \"Present\"\r\n            SourcePath = $SourcePath\r\n            DestinationPath = $DestinationPath\r\n            Type = \"Directory\"\r\n            Requires = \"[WindowsFeature]FriendlyName\"\r\n        }       \r\n    }\r\n}"), 0);
		private static readonly List<ISESnippet> defaultSnippets = ISESnippetDefaultSnippets.InitializeDefaultSnippets();
		internal static List<ISESnippet> Snippets
		{
			get
			{
				return ISESnippetDefaultSnippets.defaultSnippets;
			}
		}
		public static ISESnippet GetFromDisplayName(string displayName)
		{
			ISESnippet result = null;
			foreach (ISESnippet current in ISESnippetDefaultSnippets.defaultSnippets)
			{
				if (string.Compare(displayName, current.DisplayTitle, StringComparison.Ordinal) == 0)
				{
					result = current;
					break;
				}
			}
			return result;
		}
		private static List<ISESnippet> InitializeDefaultSnippets()
		{
			return new List<ISESnippet>
			{
				ISESnippetDefaultSnippets.defaultSnippetForIf,
				ISESnippetDefaultSnippets.defaultSnippetForIfElse,
				ISESnippetDefaultSnippets.defaultSnippetForForLoop,
				ISESnippetDefaultSnippets.defaultSnippetForForeach,
				ISESnippetDefaultSnippets.defaultSnippetForFunction2Param,
				ISESnippetDefaultSnippets.defaultSnippetForFunctionAdvanced,
				ISESnippetDefaultSnippets.defaultSnippetForFunctionAdvancedBig,
				ISESnippetDefaultSnippets.defaultSnippetForSwitch,
				ISESnippetDefaultSnippets.defaultSnippetForWhile,
				ISESnippetDefaultSnippets.defaultSnippetForDoWhile,
				ISESnippetDefaultSnippets.defaultSnippetForDoUntil,
				ISESnippetDefaultSnippets.defaultSnippetForTryCatchFinally,
				ISESnippetDefaultSnippets.defaultSnippetForTryFinally,
				ISESnippetDefaultSnippets.defaultSnippetForComment,
				ISESnippetDefaultSnippets.workflowInlineScriptSnippet,
				ISESnippetDefaultSnippets.workflowParallelSnippet,
				ISESnippetDefaultSnippets.workflowSequenceSnippet,
				ISESnippetDefaultSnippets.workflowForEachParallelSnippet,
				ISESnippetDefaultSnippets.workflowSimpleCodeSnippet,
				ISESnippetDefaultSnippets.workflowAdvancedCodeSnippet,
				ISESnippetDefaultSnippets.defaultSnippetForDSCResourceProvider,
				ISESnippetDefaultSnippets.defaultSnippetForDSCConfiguration
			};
		}
		private static string ReplaceResourceMarkers(string src)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			int num = -1;
			int i = 0;
			while (i < src.Length)
			{
				if (num != -1)
				{
					if (char.IsLetterOrDigit(src[i]))
					{
						stringBuilder2.Append(src[i]);
						i++;
					}
					else
					{
						if (i < src.Length - 2 && src[i] == '>' && src[i + 1] == '!' && src[i + 2] == '>')
						{
							if (stringBuilder2.Length == 0)
							{
								stringBuilder.Append("<!<>!>");
							}
							else
							{
								string text = GuiStrings.ResourceManager.GetString(stringBuilder2.ToString());
								if (text == null)
								{
									text = Strings.Format("<!<{0}>!>", new object[]
									{
										stringBuilder2.ToString()
									});
								}
								stringBuilder.Append(text);
							}
							stringBuilder2.Clear();
							num = -1;
							i += 3;
						}
						else
						{
							stringBuilder.Append("<!<");
							stringBuilder.Append(stringBuilder2.ToString());
							stringBuilder.Append(src[i]);
							stringBuilder2.Clear();
							num = -1;
							i++;
						}
					}
				}
				else
				{
					if (i < src.Length - 2 && src[i] == '<' && src[i + 1] == '!' && src[i + 2] == '<')
					{
						num = i;
						i += 3;
					}
					else
					{
						stringBuilder.Append(src[i]);
						i++;
					}
				}
			}
			return stringBuilder.ToString();
		}
	}
}
