using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.LanguageService
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(PowerShellConstants.LanguageName)]
    internal sealed class SmartIndentProvider : ISmartIndentProvider
    {
        private PowerShellLanguageInfo _powershellService;

        [ImportingConstructor]
        internal SmartIndentProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) 
        {
            _powershellService = (PowerShellLanguageInfo)serviceProvider.GetService(typeof(PowerShellLanguageInfo));
        }

        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            return new SmartIndent(_powershellService, textView);
        }        
    }    
}
