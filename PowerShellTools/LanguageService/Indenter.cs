using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace PowerShellTools.LanguageService
{
    [Export(typeof(ISmartIndentProvider)), ContentType(PowerShellConstants.LanguageName), Order]
    class IndenterProvider : ISmartIndentProvider
    {
        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            return new Indenter(textView);
        }
    }

    class Indenter : ISmartIndent
    {
        private ITextView _textView;

        public Indenter(ITextView textView)
        {
            _textView = textView;
        }

        public void Dispose()
        {
           
        }

        public int? GetDesiredIndentation(ITextSnapshotLine line)
        {
            try
            {
                return GetDesiredIndentationImp(line);
            }
            catch
            {
                return null;
            }
        }

        private int? GetDesiredIndentationImp(ITextSnapshotLine line)
        {
            var lineNumber = line.LineNumber;

            if (lineNumber <= 1) return 0;

            var previousLine = _textView.TextSnapshot.GetLineFromLineNumber(lineNumber - 1);
            var lineChars = previousLine.GetText().ToCharArray();

            if (lineChars.Any() && lineChars[0] == '\t')
            {
                return 4;
            }

            for (int i = 0; i < lineChars.Length; i++)
            {
                if (lineChars[i] != ' ')
                {
                    return i;
                }
            }

            return lineChars.Length;
        }
    }
}
