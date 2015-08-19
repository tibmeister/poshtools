using System;

namespace PowerShellTools
{
    public static class PowerShellConstants
    {
        public const string LanguageName = "PowerShell";
        public const string LanguageDisplayName = "PowerShell Tools";
        public const string PS1File = ".ps1";
        public const string PSD1File = ".psd1";
        public const string PSM1File = ".psm1";

        public const string EditorFactoryGuid = "53EE1FC9-2478-4DD6-9FE2-6B4E499EF22B";

        public const string PowershellOutputErrorTag = "[ERROR]";

	/// <summary>
	/// The format definition used for matched braces highlighting.
	/// </summary>
	public const string HighlightMatchedBracesFormatDefinition = "MarkerFormatDefinition/HighlightMatchedBracesFormatDefinition";
    }

    public static class LanguageUtilities
    {
        public static bool IsPowerShellFile(string fileName)
        {
            return fileName.EndsWith(PowerShellConstants.PS1File, StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(PowerShellConstants.PSD1File, StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(PowerShellConstants.PSM1File, StringComparison.OrdinalIgnoreCase);
        }
    }
}
