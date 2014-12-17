using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using PowerShellTools.Classification;

namespace PowerShellTools.Intellisense
{
    /// <summary>
    /// Class that is used for both the editor and the REPL window to manage the completion sources and
    /// completion session in the ITextBuffers. 
    /// </summary>
    internal class IntelliSenseManager
    {
        internal IOleCommandTarget NextCommandHandler;
        private readonly ITextView _textView;
        private readonly ICompletionBroker _broker;
        private ICompletionSession _activeSession;
        private readonly SVsServiceProvider _serviceProvider;
        private static readonly ILog Log = LogManager.GetLogger(typeof(IntelliSenseManager));
        private readonly bool _isRepl;
        private bool _intellisenseRunning;

        public IntelliSenseManager(ICompletionBroker broker, SVsServiceProvider provider, IOleCommandTarget commandHandler, ITextView textView)
        {
            _broker = broker;
            NextCommandHandler = commandHandler;
            _textView = textView;
            _isRepl = _textView.Properties.ContainsProperty(BufferProperties.FromRepl);
            _serviceProvider = provider;
        }
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        /// <summary>
        /// Main method used to determine how to handle keystrokes within a ITextBuffer.
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdId"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                return NextCommandHandler.Exec(ref pguidCmdGroup, nCmdId, nCmdexecopt, pvaIn, pvaOut);
            }
            //make a copy of this so we can look at it after forwarding some commands 
            var commandId = nCmdId;
            var typedChar = char.MinValue;
            //make sure the input is a char before getting it 
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdId == (uint) VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char) (ushort) Marshal.GetObjectForNativeVariant(pvaIn);
            }

            Log.DebugFormat("Typed Character: {0}", typedChar);

            //check for a commit character 
            if (nCmdId == (uint) VSConstants.VSStd2KCmdID.RETURN
                || nCmdId == (uint) VSConstants.VSStd2KCmdID.TAB
                || (char.IsWhiteSpace(typedChar)))
            {
                //check for a a selection 
                if (_activeSession != null && !_activeSession.IsDismissed)
                {
                    //if the selection is fully selected, commit the current session 
                    if (_activeSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        Log.Debug("Commit");
                        _activeSession.Commit();

                        //also, don't add the character to the buffer 
                        return VSConstants.S_OK;
                    }
                    
                    Log.Debug("Dismiss");
                    //if there is no selection, dismiss the session
                    _activeSession.Dismiss();
                }
                else if (nCmdId == (uint) VSConstants.VSStd2KCmdID.TAB && _isRepl)
                {
                    TriggerCompletion();
                    return VSConstants.S_OK;
                }
            }

            //pass along the command so the char is added to the buffer 
            int retVal = NextCommandHandler.Exec(ref pguidCmdGroup, nCmdId, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if (!typedChar.Equals(char.MinValue) && IsIntellisenseTrigger(typedChar))
            {
                TriggerCompletion();
            }
            if (!typedChar.Equals(char.MinValue) && IsFilterTrigger(typedChar))
            {
                if (_activeSession != null)
                {
                    if (_activeSession.IsStarted)
                    {
                        Log.Debug("Filter");
                        _activeSession.Filter();
                    }
                }
            }
            else if (commandId == (uint) VSConstants.VSStd2KCmdID.BACKSPACE //redo the filter if there is a deletion
                     || commandId == (uint) VSConstants.VSStd2KCmdID.DELETE)
            {
                if (_activeSession != null && !_activeSession.IsDismissed)
                {
                    Log.Debug("Filter");
                    _activeSession.Filter();
                }
                    
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;
        }

        /// <summary>
        /// Triggers an IntelliSense session. This is done in a seperate thread than the UI to allow
        /// the user to continue typing. 
        /// </summary>
        private void TriggerCompletion()
        {
            var caretPosition = (int) _textView.Caret.Position.BufferPosition;
            var thread = new Thread(() =>
            {
                try
                {
                    var line = _textView.Caret.Position.BufferPosition.GetContainingLine();
                    var caretInLine = (caretPosition - line.Start);
                    var text = line.GetText().Substring(0, caretInLine);
                    StartIntelliSense(line.Start, caretPosition, text);
                }
                catch (Exception ex)
                {
                    Log.Warn("Failed to start IntelliSense", ex);
                    _intellisenseRunning = false;
                }

            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void GetCommandCompletionParameters(int caretPosition, out Ast ast, out Token[] tokens, out IScriptPosition cursorPosition)
        {
            ITrackingSpan trackingSpan;
            _textView.TextBuffer.Properties.TryGetProperty(BufferProperties.Ast, out ast);
            _textView.TextBuffer.Properties.TryGetProperty(BufferProperties.Tokens, out tokens);
            _textView.TextBuffer.Properties.TryGetProperty(BufferProperties.SpanTokenized, out trackingSpan);

            // No ast or tokens available on the buffer. Re-tokenize.
            if (ast == null || tokens == null)
            {
                var trackingSpan2 = _textView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(0, _textView.TextBuffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeInclusive);
                var text = trackingSpan2.GetText(_textView.TextBuffer.CurrentSnapshot);
                ParseError[] array;
                ast = Tokenize(text, out tokens, out array);
            }
            if (ast != null)
            {
                //HACK: Clone with a new offset using private method... 
                var type = ast.Extent.StartScriptPosition.GetType();
                var method = type.GetMethod("CloneWithNewOffset", BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] {typeof (int)}, null);

                cursorPosition = (IScriptPosition)method.Invoke(ast.Extent.StartScriptPosition, new object[]{caretPosition});
                return;
            }
            cursorPosition = null;
        }

        internal static Ast Tokenize(string script, out Token[] tokens, out ParseError[] errors)
        {
            Ast result;
            try
            {
                Token[] array;
                Ast ast = Parser.ParseInput(script, out array, out errors);
                tokens = new Token[array.Length - 1];
                Array.Copy(array, tokens, tokens.Length);
                result = ast;
            }
            catch (RuntimeException ex)
            {
                var parseError = new ParseError(new EmptyScriptExtent(), ex.ErrorRecord.FullyQualifiedErrorId, ex.Message);
                errors = new []{parseError};
                tokens = new Token[0];
                result = null;
            }
            return result;
        }

        private void StartIntelliSense(int lineStartPosition, int caretPosition, string lineTextUpToCaret)
        {
            if (_intellisenseRunning) return;

            _intellisenseRunning = true;
            var statusBar = (IVsStatusbar)PowerShellToolsPackage.Instance.GetService(typeof(SVsStatusbar));
            statusBar.SetText("Running IntelliSense...");
            var sw = new Stopwatch();
            sw.Start();

            Ast ast;
            Token[] tokens;
            IScriptPosition cursorPosition;
            GetCommandCompletionParameters(caretPosition, out ast, out tokens, out cursorPosition);
            if (ast == null)
            {
                return;
            }

            var ps = PowerShell.Create();
            ps.Runspace = PowerShellToolsPackage.Debugger.Runspace;

            var commandCompletion = CommandCompletion.CompleteInput(ast, tokens, cursorPosition, null, ps); 
            
            var line = _textView.Caret.Position.BufferPosition.GetContainingLine();
            var caretInLine = (caretPosition - line.Start);
            var text = line.GetText().Substring(0, caretInLine);

            IList<CompletionResult> list = commandCompletion.CompletionMatches;
            if (string.Equals(lineTextUpToCaret, text, StringComparison.Ordinal) && list.Count != 0)
            {
                if (list.Count != 0)
                {
                    try
                    {
                        IntellisenseDone(commandCompletion.CompletionMatches, lineStartPosition,
                            commandCompletion.ReplacementIndex + 0, commandCompletion.ReplacementLength, caretPosition);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Failed to start IntelliSense.", ex);
                    }
                }
            }

            statusBar.SetText(String.Format("IntelliSense complete in {0:0.00} seconds...", sw.Elapsed.TotalSeconds));
            _intellisenseRunning = false;
        }

        internal void IntellisenseDone(IList<CompletionResult> completionResults, int lineStartPosition, int replacementIndex, int replacementLength, int startCaretPosition)
        {
            var textBuffer = _textView.TextBuffer;
            var length = replacementIndex - lineStartPosition;
            if (!SpanArgumentsAreValid(textBuffer.CurrentSnapshot, replacementIndex, replacementLength) || !SpanArgumentsAreValid(textBuffer.CurrentSnapshot, lineStartPosition, length))
            {
                return;
            }
            var lastWordReplacementSpan = textBuffer.CurrentSnapshot.CreateTrackingSpan(replacementIndex, replacementLength, SpanTrackingMode.EdgeInclusive);
            var lineUpToReplacementSpan = textBuffer.CurrentSnapshot.CreateTrackingSpan(lineStartPosition, length, SpanTrackingMode.EdgeExclusive);

            var triggerPoint = textBuffer.CurrentSnapshot.CreateTrackingPoint(startCaretPosition, PointTrackingMode.Positive);
            textBuffer.Properties.AddProperty(typeof(IList<CompletionResult>), completionResults);
            textBuffer.Properties.AddProperty(BufferProperties.LastWordReplacementSpan, lastWordReplacementSpan);
            textBuffer.Properties.AddProperty(BufferProperties.LineUpToReplacementSpan, lineUpToReplacementSpan);

            Log.Debug("Dismissing all sessions...");
            _broker.DismissAllSessions(_textView);

            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
            {
                StartSession(triggerPoint);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => StartSession(triggerPoint));
            }

            textBuffer.Properties.RemoveProperty(typeof(IList<CompletionResult>));
            textBuffer.Properties.RemoveProperty(BufferProperties.LastWordReplacementSpan);
            textBuffer.Properties.RemoveProperty(BufferProperties.LineUpToReplacementSpan);
        }

        private void StartSession(ITrackingPoint triggerPoint)
        {
            Log.Debug("Creating new completion session...");
            _activeSession = _broker.CreateCompletionSession(_textView, triggerPoint, true);
            _activeSession.Properties.AddProperty(BufferProperties.SessionOriginIntellisense, "Intellisense");
            _activeSession.Dismissed += CompletionSession_Dismissed;
            _activeSession.Start();
        }


        private void CompletionSession_Dismissed(object sender, EventArgs e)
        {
            Log.Debug("Session Dismissed.");
            _activeSession = null;
        }

        internal static bool SpanArgumentsAreValid(ITextSnapshot snapshot, int start, int length)
        {
            return start >= 0 && length >= 0 && start + length <= snapshot.Length;
        }

        /// <summary>
        /// Determines whether a typed character should cause the completion source list to filter.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsFilterTrigger(char ch)
        {
            Log.DebugFormat("IsFilterTrigger: [{0}]", ch);
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
        }

        /// <summary>
        /// Determines whether a typed character should cause the manager to trigger the intellisense drop down.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsIntellisenseTrigger(char ch)
        {
            Log.DebugFormat("IsIntellisenseTrigger: [{0}]", ch);
            return ch == '-' || ch == '$' || ch == '.' || ch == ':' || ch == '\\';
        }
    }

    [Serializable]
    internal sealed class EmptyScriptExtent : IScriptExtent
    {
        public string File
        {
            get
            {
                return null;
            }
        }
        public IScriptPosition StartScriptPosition
        {
            get
            {
                return new EmptyScriptPosition();
            }
        }
        public IScriptPosition EndScriptPosition
        {
            get
            {
                return new EmptyScriptPosition();
            }
        }
        public int StartLineNumber
        {
            get
            {
                return 0;
            }
        }
        public int StartColumnNumber
        {
            get
            {
                return 0;
            }
        }
        public int EndLineNumber
        {
            get
            {
                return 0;
            }
        }
        public int EndColumnNumber
        {
            get
            {
                return 0;
            }
        }
        public int StartOffset
        {
            get
            {
                return 0;
            }
        }
        public int EndOffset
        {
            get
            {
                return 0;
            }
        }
        public string Text
        {
            get
            {
                return "";
            }
        }
        public override bool Equals(object obj)
        {
            var scriptExtent = obj as IScriptExtent;
            return scriptExtent != null && (string.IsNullOrEmpty(scriptExtent.File) && scriptExtent.StartLineNumber == StartLineNumber && scriptExtent.StartColumnNumber == StartColumnNumber && scriptExtent.EndLineNumber == EndLineNumber && scriptExtent.EndColumnNumber == EndColumnNumber && string.IsNullOrEmpty(scriptExtent.Text));
        }
    }

    [Serializable]
    internal sealed class EmptyScriptPosition : IScriptPosition
    {
        public string File
        {
            get
            {
                return null;
            }
        }
        public int LineNumber
        {
            get
            {
                return 0;
            }
        }
        public int ColumnNumber
        {
            get
            {
                return 0;
            }
        }
        public int Offset
        {
            get
            {
                return 0;
            }
        }
        public string Line
        {
            get
            {
                return "";
            }
        }
        public string GetFullScript()
        {
            return null;
        }
    }
}