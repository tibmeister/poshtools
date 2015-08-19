using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.Classification
{
    [ContentType("TextOutput"), Export(typeof(IViewTaggerProvider)), TagType(typeof(ErrorTag)), ContentType("PowerShell")]
    internal class PowerShellErrorTaggerProvider : IViewTaggerProvider
    {
	[Import]
	internal IDependencyValidator _validator;

	public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
	{
	    if (!_validator.Validate()) return null;

	    return buffer.Properties.GetOrCreateSingletonProperty(typeof(PowerShellErrorTagger), () => new PowerShellErrorTagger(buffer) as ITagger<T>);
	}
    }
}
