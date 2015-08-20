using System;
using System.Linq;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using PowerShellTools.Classification;
using PowerShellTools.Repl;

namespace PowerShellTools.Intellisense
{
    /// <summary>
    /// Command handler in the command chain that is used for complete brace/quotes for both the editor and REPL window.
    /// </summary>
    internal sealed class AutoCompletionController : IOleCommandTarget
    {

        private readonly ITextView _textView;
        private readonly IEditorOperations _editorOperations;
        private readonly ITextUndoHistory _undoHistory;
        private readonly SVsServiceProvider _serviceProvider;        
        private int _autoCompleteCount;
        private static readonly ILog Log = LogManager.GetLogger(typeof(AutoCompletionController));

        public AutoCompletionController(ITextView textView,
                                         IEditorOperations editorOperations,
                                         ITextUndoHistory undoHistory,
                                         SVsServiceProvider serviceProvider)
        {            
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }
            if (editorOperations == null)
            {
                throw new ArgumentNullException("editorOperations");
            }
            if (undoHistory == null)
            {
                throw new ArgumentNullException("undoHistory");
            }
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            _textView = textView;
            _editorOperations = editorOperations;
            _undoHistory = undoHistory;
            _serviceProvider = serviceProvider;
            _autoCompleteCount = 0;
        }

        public IOleCommandTarget NextCommandHandler { get; set; }

        private bool IsLastCmdAutoComplete
        {
            get
            {
                return _autoCompleteCount > 0;
            }
            set
            {
                if (value)
                {
                    _autoCompleteCount++;
                }
                else
                {
                    _autoCompleteCount = 0;
                }
            }
        }

        #region IOleCommandTarget implementation

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var command = (VSConstants.VSStd2KCmdID)nCmdID;

            if (VsShellUtilities.IsInAutomationFunction(_serviceProvider) ||
                pguidCmdGroup != VSConstants.VSStd2K ||
                !_textView.Selection.IsEmpty ||
                Utilities.IsCaretInCommentArea(_textView) ||
                (!(command == VSConstants.VSStd2KCmdID.BACKSPACE && this.IsLastCmdAutoComplete) && 
                 IsInStringArea()))
            {
                // Auto completion shouldn't take effect when
                // 1. In automation function
                // 2. The cmd group is not fit
                // 3. There is text selection.
                // 4. In comment blocks or line.
                // 5. In string literal with one exception, which is BACKSPACE right after auto complete quotes.
                return NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            char typedChar = Char.MinValue;
            if (command == VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            return ProcessKeystroke(command, typedChar) == VSConstants.S_OK ? VSConstants.S_OK : NextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return NextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        #endregion       

        internal int ProcessKeystroke(VSConstants.VSStd2KCmdID command, char typedChar = Char.MinValue)
        {
            switch (command)
            {
                case VSConstants.VSStd2KCmdID.TYPECHAR:
                    // If we processed the typed left brace/quotes, no need to pass along the command as the char is already added to the buffer.
                    if (Utilities.IsQuotes(typedChar))
                    {
                        if (this.IsLastCmdAutoComplete && IsTypedCharEqualsNextChar(typedChar))
                        {
                            ProcessTypedRightBraceOrQuotes(typedChar);
                            SetAutoCompleteState(false);
                            return VSConstants.S_OK;
                        }
                        else
                        {
                            CompleteBraceOrQuotes(typedChar);
                            SetAutoCompleteState(true);
                            return VSConstants.S_OK;
                        }
                    }

                    if (Utilities.IsLeftBraceOrQuotes(typedChar))
                    {
                        CompleteBraceOrQuotes(typedChar);
                        SetAutoCompleteState(true);
                        return VSConstants.S_OK;
                    }
                    else if (Utilities.IsRightBraceOrQuotes(typedChar) && ProcessTypedRightBraceOrQuotes(typedChar))
                    {
                        // If this right brace/quotes is typed right after typing left brace/quotes,
                        // we just move the caret to the right side of the right brace/quotes and return.
                        // This means we will not add the typed right brace/quotes to the text buffer.
                        _autoCompleteCount--;
                        return VSConstants.S_OK;
                    }
                    break;

                case VSConstants.VSStd2KCmdID.RETURN:
                    // Return in Repl windows would execute the current command 
                    if (_textView.TextBuffer.ContentType.TypeName.Equals(ReplConstants.ReplContentTypeName, StringComparison.Ordinal))
                    {
                        SetAutoCompleteState(false);
                        break;
                    }
                    if (ProcessReturnKey())
                    {
                        SetAutoCompleteState(false);
                        return VSConstants.S_OK;
                    }
                    SetAutoCompleteState(false);
                    break;

                case VSConstants.VSStd2KCmdID.BACKSPACE:
                    // As there are no undo history preserved for REPL window, default action is applied to Backspace.
                    if (_textView.TextBuffer.ContentType.TypeName.Equals(ReplConstants.ReplContentTypeName, StringComparison.Ordinal))
                    {
                        SetAutoCompleteState(false);
                        break;
                    }

                    if (ProcessBackspaceKey())
                    {
                        _autoCompleteCount--;
                        return VSConstants.S_OK;
                    }
                    SetAutoCompleteState(false);
                    break;

                case VSConstants.VSStd2KCmdID.DELETE:
                case VSConstants.VSStd2KCmdID.UNDO:
                case VSConstants.VSStd2KCmdID.CUT:
                case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                case VSConstants.VSStd2KCmdID.LEFT:
                case VSConstants.VSStd2KCmdID.RIGHT:
                case VSConstants.VSStd2KCmdID.UP:
                case VSConstants.VSStd2KCmdID.DOWN:
                    SetAutoCompleteState(false);
                    break;
                default:
                    break;
            }
            return VSConstants.S_FALSE;
        }

        internal void SetAutoCompleteState(bool isAutoComplete)
        {
            this.IsLastCmdAutoComplete = isAutoComplete;
        }
        
        private bool IsInStringArea()
        {
            ITextBuffer currentActiveBuffer;
            int currentPosition = Utilities.GetCurrentBufferPosition(_textView, out currentActiveBuffer);
            if (currentPosition < 0 || currentPosition > currentActiveBuffer.CurrentSnapshot.Length)
            {
                return false;
            }
            return Utilities.IsInStringArea(currentPosition, currentActiveBuffer);
        }
        
        /// <summary>
        /// Complete the left brace/quotes with matched brace/quotes
        /// </summary>
        /// <param name="leftBraceOrQuotes">The left brace/quotes to be completed.</param>
        private void CompleteBraceOrQuotes(char leftBraceOrQuotes)
        {
            var typedCharToString = leftBraceOrQuotes.ToString();

            // Step 1, create an undo transaction in case after typing the left brace/quotes user then click BACKSPACE to delete it. 
            //         In that case, we need to undo this transaction, which will delete not only the left brace/quotes but also the filled matched brace/quotes
            // Step 2, insert the typed char which is the left brace/quotes
            // Step 3, insert a matched brace/quotes and then move caret to the previous char, meaning the caret ends up in the middle of the brace/quotes pair.
            using (var undo = _undoHistory.CreateTransaction("Fill in " + typedCharToString))
            {
                _editorOperations.AddBeforeTextBufferChangePrimitive();

                _editorOperations.InsertText(typedCharToString);
                _editorOperations.InsertText(Utilities.GetCloseBraceOrQuotes(leftBraceOrQuotes).ToString());
                _editorOperations.MoveToPreviousCharacter(false);

                _editorOperations.AddAfterTextBufferChangePrimitive();
                undo.Complete();
            }
        }

        private bool ProcessTypedRightBraceOrQuotes(char typedChar)
        {
            if (this.IsLastCmdAutoComplete)
            {
                _editorOperations.MoveToNextCharacter(false);
            }

            return this.IsLastCmdAutoComplete;
        }

        private bool ProcessReturnKey()
        {
            bool isReturnKeyProcessed = this.IsLastCmdAutoComplete && IsCaretInMiddleOfGroup();
            if (isReturnKeyProcessed)
            {
                using (var undo = _undoHistory.CreateTransaction("Insert new line."))
                {
                    _editorOperations.AddBeforeTextBufferChangePrimitive();

                    _editorOperations.InsertNewLine();
                    _editorOperations.MoveLineUp(false);
                    _editorOperations.MoveToEndOfLine(false);
                    _editorOperations.InsertNewLine();

                    _editorOperations.AddAfterTextBufferChangePrimitive();
                    undo.Complete();
                }
            }

            return isReturnKeyProcessed;
        }

        private bool ProcessBackspaceKey()
        {
            var isBackspaceKeyProcessed = this.IsLastCmdAutoComplete && IsCaretInMiddleOfPairedBraceOrQuotes();
            if (isBackspaceKeyProcessed)
            {
                _undoHistory.Undo(1);
            }
            return isBackspaceKeyProcessed;
        }

        private bool IsCaretInMiddleOfPairedBraceOrQuotes()
        {
            int currentCaret = _textView.Caret.Position.BufferPosition.Position;
            return IsPreviousCharLeftBraceOrQuotes(currentCaret) && IsNextCharRightBraceOrQuotes(currentCaret);
        }

        private bool IsPreviousCharLeftBraceOrQuotes(int currentCaret)
        {
            if (currentCaret == 0) return false;

            ITrackingPoint previousCharPosition = _textView.TextSnapshot.CreateTrackingPoint(currentCaret - 1, PointTrackingMode.Positive);
            char previousChar = previousCharPosition.GetCharacter(_textView.TextSnapshot);
            return Utilities.IsLeftBraceOrQuotes(previousChar);
        }

        private bool IsTypedCharEqualsNextChar(char currentChar)
        {
            int currentCaret = _textView.Caret.Position.BufferPosition.Position;
            if (currentCaret >= _textView.TextSnapshot.Length) return false;

            ITrackingPoint nextCharPosition = _textView.TextSnapshot.CreateTrackingPoint(currentCaret, PointTrackingMode.Positive);
            char nextChar = nextCharPosition.GetCharacter(_textView.TextSnapshot);
            return currentChar == nextChar;
        }

        private bool IsNextCharRightBraceOrQuotes(int currentCaret)
        {
            if (currentCaret >= _textView.TextSnapshot.Length) return false;

            ITrackingPoint nextCharPosition = _textView.TextSnapshot.CreateTrackingPoint(currentCaret, PointTrackingMode.Positive);
            char nextChar = nextCharPosition.GetCharacter(_textView.TextSnapshot);
            return Utilities.IsRightBraceOrQuotes(nextChar);
        }

        private bool IsCaretInMiddleOfGroup()
        {
            int currentCaret = _textView.Caret.Position.BufferPosition.Position;

            // Get preceding char
            if (currentCaret == 0) return false;
            ITrackingPoint precedingCharPosition = _textView.TextSnapshot.CreateTrackingPoint(currentCaret - 1, PointTrackingMode.Positive);
            char precedingChar = precedingCharPosition.GetCharacter(_textView.TextSnapshot);

            // Get succeeding char
            if (currentCaret >= _textView.TextSnapshot.Length) return false;
            ITrackingPoint succeedingCharPosition = _textView.TextSnapshot.CreateTrackingPoint(currentCaret, PointTrackingMode.Positive);
            char succeedingChar = succeedingCharPosition.GetCharacter(_textView.TextSnapshot);

            // Determine if the two chars are paired.

            return Utilities.IsGroupStart(precedingChar) && Utilities.IsGroupEnd(succeedingChar) && precedingChar == Utilities.GetPairedBrace(succeedingChar);
        }

        private void DeleteRightBrace()
        {
            using (var undo = _undoHistory.CreateTransaction("Delete right brace."))
            {
                _editorOperations.AddBeforeTextBufferChangePrimitive();

                _editorOperations.Delete();

                _editorOperations.AddAfterTextBufferChangePrimitive();
                undo.Complete();
            }
        }
    }
}
