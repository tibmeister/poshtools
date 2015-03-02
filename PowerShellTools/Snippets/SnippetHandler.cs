using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using MSXML;

namespace PowerShellTools.Snippets
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("snippets")]
    [ContentType("PowerShell")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class SnippetHandlerProvider : IVsTextViewCreationListener
    {
       // internal const string LanguageServiceGuidStr = "AD4D401C-11EA-431F-A412-FAB167156206";

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            textView.Properties.GetOrCreateSingletonProperty(() => new SnippetHandler(textViewAdapter, this));
        }
    }

    internal class SnippetHandler : IOleCommandTarget, IVsExpansionClient
    {
        readonly IVsTextView _mVsTextView;
        IVsExpansionManager _mExManager;
        IVsExpansionSession _mExSession;
        private readonly IOleCommandTarget _mNextCommandHandler;
        private readonly SnippetHandlerProvider _mProvider;

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(_mProvider.ServiceProvider))
            {
                return _mNextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            var retVal =  _mNextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            if (nCmdID != (uint) VSConstants.VSStd2KCmdID.INSERTSNIPPET) return retVal;

            var textManager = (IVsTextManager2)_mProvider.ServiceProvider.GetService(typeof(SVsTextManager));

            textManager.GetExpansionManager(out _mExManager);

            try
            {
                _mExManager.InvokeInsertionUI(
                    _mVsTextView,
                    this, //the expansion client 
                    new Guid( GuidList.PowerShellLanguage),
                    null, //use all snippet types
                    0, //number of types (0 for all)
                    0, //ignored if iCountTypes == 0 
                    null, //use all snippet kinds
                    0, //use all snippet kinds
                    0, //ignored if iCountTypes == 0 
                    "Insert snippet", //the text to show in the prompt 
                    string.Empty); //only the ENTER key causes insert  
            }
            catch (Exception)
            {
                return VSConstants.E_FAIL;
            }

            return VSConstants.S_OK;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (VsShellUtilities.IsInAutomationFunction(_mProvider.ServiceProvider))
                return _mNextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            if (pguidCmdGroup != VSConstants.VSStd2K || cCmds <= 0)
                return _mNextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            if (prgCmds[0].cmdID != (uint) VSConstants.VSStd2KCmdID.INSERTSNIPPET)
                return _mNextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (int)Constants.MSOCMDF_ENABLED | (int)Constants.MSOCMDF_SUPPORTED;
            return VSConstants.S_OK;
        }

        internal SnippetHandler(IVsTextView textViewAdapter, SnippetHandlerProvider provider)
        {
            _mVsTextView = textViewAdapter;
            _mProvider = provider;
            //get the text manager from the service provider
            var textManager = (IVsTextManager2)_mProvider.ServiceProvider.GetService(typeof(SVsTextManager));
            textManager.GetExpansionManager(out _mExManager);
            _mExSession = null;

            //add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out _mNextCommandHandler);
        }

        public int EndExpansion()
        {
            _mExSession = null;
            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
        {
            return VSConstants.S_OK;
        }

        public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            pFunc = null;
            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
        {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
        {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeInsertion(IVsExpansionSession pSession)
        {
            return VSConstants.S_OK;
        }

        public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts)
        {
            return VSConstants.S_OK;
        }

        public int OnItemChosen(string pszTitle, string pszPath)
        {
            InsertAnyExpansion(null, pszTitle, pszPath);
            return VSConstants.S_OK;
        }

        private bool InsertAnyExpansion(string shortcut, string title, string path)
        {
            //first get the location of the caret, and set up a TextSpan 
            int endColumn, startLine;
            //get the column number from  the IVsTextView, not the ITextView
            _mVsTextView.GetCaretPos(out startLine, out endColumn);

            var addSpan = new TextSpan();
            addSpan.iStartIndex = endColumn;
            addSpan.iEndIndex = endColumn;
            addSpan.iStartLine = startLine;
            addSpan.iEndLine = startLine;

            if (shortcut != null) //get the expansion from the shortcut
            {
                //reset the TextSpan to the width of the shortcut,  
                //because we're going to replace the shortcut with the expansion
                addSpan.iStartIndex = addSpan.iEndIndex - shortcut.Length;

                _mExManager.GetExpansionByShortcut(
                    this,
                    new Guid(GuidList.PowerShellLanguage),
                    shortcut,
                    _mVsTextView,
                    new[] { addSpan },
                    0,
                    out path,
                    out title);
            }

            if (title == null || path == null) return false;

            IVsTextLines textLines;
            _mVsTextView.GetBuffer(out textLines);
            var bufferExpansion = (IVsExpansion)textLines;

            if (bufferExpansion == null) return false;

            var hr = bufferExpansion.InsertNamedExpansion(
                title,
                path,
                addSpan,
                this,
                new Guid(GuidList.PowerShellLanguage),
                0,
                out _mExSession);

            return VSConstants.S_OK == hr;
        }
    }

    
}
