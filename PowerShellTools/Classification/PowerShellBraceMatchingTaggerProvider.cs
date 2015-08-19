using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(PowerShellConstants.LanguageName)]
    [TagType(typeof(TextMarkerTag))]
    internal sealed class PowerShellBraceMatchingTaggerProvider : IViewTaggerProvider
    {
	[Import]
	internal IDependencyValidator _validator;

	public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
	{
	    if (textView == null || !_validator.Validate()) return null;

	    return buffer.Properties.GetOrCreateSingletonProperty(typeof(PowerShellBraceMatchingTagger), () => new PowerShellBraceMatchingTagger(textView, buffer) as ITagger<T>);
	}
    }
}
