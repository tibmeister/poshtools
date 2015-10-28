using System;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudioTools;
using PowerShellTools.Classification;
using PowerShellTools.Common.Logging;

namespace PowerShellTools.LanguageService
{
    internal sealed class EditFilter : IOleCommandTarget
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (EditFilter));
        private readonly ITextView _textView;
        private readonly IEditorOperations _editorOps;
        private IOleCommandTarget _next;
        private IVsStatusbar _statusBar;

        public EditFilter(ITextView textView, IEditorOperations editorOps, IVsStatusbar statusBar)
        {
            _textView = textView;
            _textView.Properties[typeof(EditFilter)] = this;
            _editorOps = editorOps;
            _statusBar = statusBar;
        }

        internal void AttachKeyboardFilter(IVsTextView vsTextView)
        {
            if (_next == null)
            {
                ErrorHandler.ThrowOnFailure(vsTextView.AddCommandFilter(this, out _next));
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == CommonConstants.Std2KCmdGroupGuid)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch ((VSConstants.VSStd2KCmdID)prgCmds[i].cmdID)
                    {
                        case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                        case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                        case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                        case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID)
                    {
                        case VSConstants.VSStd97CmdID.GotoDefn:
                        case VSConstants.VSStd97CmdID.F1Help:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }

            return _next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == CommonConstants.Std2KCmdGroupGuid)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                        if (EditorExtensions.CommentOrUncommentBlock(_textView, comment: true))
                        {
                            return VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                    case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                        if (EditorExtensions.CommentOrUncommentBlock(_textView, comment: false))
                        {
                            return VSConstants.S_OK;
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID) nCmdID)
                {
                    case VSConstants.VSStd97CmdID.GotoDefn:
                        GoToDefinition();
                        return VSConstants.S_OK;
                    case VSConstants.VSStd97CmdID.F1Help:
                        GetHelp();
                        return VSConstants.S_OK;
                }
            }

            return _next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void GetHelp()
        {
            Ast script;
            _textView.TextBuffer.Properties.TryGetProperty(BufferProperties.Ast, out script);

            var caretPosition = _textView.Caret.Position.BufferPosition.Position;

            var reference = script.Find(node =>
            node is CommandAst &&
            caretPosition >= node.Extent.StartOffset &&
            caretPosition <= node.Extent.EndOffset, true) as CommandAst;

            if (reference == null) return;

            Task.Run(() =>
            {
                    string commandName = string.Empty;
                    try
                    {
                        commandName = reference.GetCommandName();
                        _statusBar.SetText(string.Format(Resources.GetHelp_Searching, commandName));
                        var errors = PowerShellToolsPackage.DebuggingService.Execute(string.Format("Get-Help {0} -Online", commandName));

                        if (!errors)
                        {
                            _statusBar.SetText(string.Format(CultureInfo.CurrentCulture, Resources.GetHelp_HelpNotFound, commandName));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusBar.SetText(string.Format(CultureInfo.CurrentCulture, Resources.GetHelp_HelpNotFound, commandName));
                        Log.Warn(string.Format("Failed to find help for command '{0}'", reference), ex);
                    }
                
            });

        }

        private void GoToDefinition()
        {
            Ast script;
            _textView.TextBuffer.Properties.TryGetProperty(BufferProperties.Ast, out script);
            var definitions = NavigationExtensions.FindFunctionDefinitions(script, _textView.TextBuffer.CurrentSnapshot, _textView.Caret.Position.BufferPosition.Position);

            if (definitions != null && definitions.Any())
            {
                if (definitions.Count() > 1 && _statusBar != null)
                {
                    // If outside the scope of the call, there is no way to determine which function definition is used until run-time.
                    // Letting the user know in the status bar, and we will arbitrarily navigate to the first definition
                    _statusBar.SetText(Resources.GoToDefinitionAmbiguousMessage);
                }

                NavigationExtensions.NavigateToFunctionDefinition(_textView, definitions.First());
            }
            else
            {
                var message = string.Format("{0}{1}{2}{3}", Resources.GoToDefinitionName, Environment.NewLine, Environment.NewLine, Resources.GoToDefinitionFailureMessage);
                MessageBox.Show(message, Resources.MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}