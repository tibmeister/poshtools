using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using PowerShellTools.Intellisense;
using PowerShellTools.Repl;

namespace PowerShellTools.Test.IntelliSense
{
    [TestClass]
    public class AutoCompletionControllerUnitTests
    {
        private string _mockedScript;
        private int _mockedCaret;

        [TestMethod]
        public void ShouldCompleteBrace()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(false);

            _mockedScript = @"MyFunc";
            _mockedCaret = 6;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, '(');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldCompleteQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(false);

            _mockedScript = @"MyFunc";
            _mockedCaret = 6;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, '\"');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc""""", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldMoveToRightByTypingCompletedBrackets()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc[]";
            _mockedCaret = 7;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, ']');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc[]", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 8);
        }

        [TestMethod]
        public void ShouldMoveToRightByTypingCompletedQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc""""";
            _mockedCaret = 7;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, '\"');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc""""", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 8);
        }

        [TestMethod]
        public void ShouldMoveToRightByTypingCharsAndCompletedQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc""""";
            _mockedCaret = 7;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, 'a');
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, 'b');
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, 'c');
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, '\"');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
        }

        [TestMethod]
        public void ShouldKeepMovingToRightByTypingCompletedBracesInNestedBracePairs()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);
            autoCompletionController.SetAutoCompleteState(true);
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc{([])}";
            _mockedCaret = 9;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, ']');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc{([])}", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 10);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, ']');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc{([])}", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 11);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, ']');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc{([])}", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 12);
        }

        [TestMethod]
        public void ShouldCompletedQuotesWhenNextCharIsNotSameQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, '\"');
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc("""")", _mockedScript);
            Assert.AreEqual<int>(_mockedCaret, 8);
        }

        [TestMethod]
        public void ShouldNotAddCloseBraceToBufferWhenLastCmdIsNotAutoComplete()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(false);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;

            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.TYPECHAR, ')');
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
        }

        [TestMethod]
        public void ShouldNotProcessReturnKeyWhenInRepl()
        {
            var autoCompletionController = FakeAutoCompletionController(ReplConstants.ReplContentTypeName);
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.RETURN);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
        }

        [TestMethod]
        public void ShouldProcessReturnKeyWhenInPowershellCode()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc{}";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.RETURN);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>("MyFunc{\r\n\r\n}", _mockedScript);
            Assert.AreEqual<int>(9, _mockedCaret);
        }

        [TestMethod]
        public void ShouldNotProcessReturnKeyWhenLastCmdIsNotAutoComplete()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(false);

            _mockedScript = @"MyFunc{}";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.RETURN);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
            Assert.AreEqual<string>(@"MyFunc{}", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldNotProcessReturnKeyWhenCursorNotInGroups()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc[]";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.RETURN);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
            Assert.AreEqual<string>(@"MyFunc[]", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldNotProcessBackspaceKeyWhenInRepl()
        {
            var autoCompletionController = FakeAutoCompletionController(ReplConstants.ReplContentTypeName);
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldNotDoBackspaceWhenLastCmdIsNotAutoComplete()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(false);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldNotDoBackspaceWhenNotInMiddleOfPairedBracesOrQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc(";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_FALSE, actual);
            Assert.AreEqual<string>(@"MyFunc(", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);
        }

        [TestMethod]
        public void ShouldProcessBackspaceWhenBackspaceRightAfterAutoCompleteBraces()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc()";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc", _mockedScript);
            Assert.AreEqual<int>(6, _mockedCaret);
        }

        [TestMethod]
        public void ShouldProcessBackspaceWhenBackspaceRightAfterAutoCompleteQuotes()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc""""";
            _mockedCaret = 7;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc", _mockedScript);
            Assert.AreEqual<int>(6, _mockedCaret);
        }

        [TestMethod]
        public void ShouldProcessBackspaceInNestedAutoCompletions()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc(())";
            _mockedCaret = 8;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc", _mockedScript);
            Assert.AreEqual<int>(6, _mockedCaret);
        }

        [TestMethod]
        public void ShouldProcessBackspaceWhenInStringButRightAfterAutoCompletions()
        {
            var autoCompletionController = FakeAutoCompletionController();
            autoCompletionController.SetAutoCompleteState(true);
            autoCompletionController.SetAutoCompleteState(true);

            _mockedScript = @"MyFunc("""")";
            _mockedCaret = 8;
            int actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc()", _mockedScript);
            Assert.AreEqual<int>(7, _mockedCaret);

            actual = autoCompletionController.ProcessKeystroke(VSConstants.VSStd2KCmdID.BACKSPACE);
            Assert.AreEqual<int>(VSConstants.S_OK, actual);
            Assert.AreEqual<string>(@"MyFunc", _mockedScript);
            Assert.AreEqual<int>(6, _mockedCaret);
        }
        
        private AutoCompletionController FakeAutoCompletionController(string contentType = PowerShellConstants.LanguageName)
        {
            var textView = TextViewMock(contentType);
            var editorOperations = EditorOperationsMock();
            var textUndoHistory = TextUndoHistoryMock();
            Mock<SVsServiceProvider> serviceProvider = new Mock<SVsServiceProvider>();
            AutoCompletionController autoCompletionController = new AutoCompletionController(textView.Object,
                                                                                             editorOperations.Object,
                                                                                             textUndoHistory.Object,
                                                                                             serviceProvider.Object);
            return autoCompletionController;
        }

        private Mock<ITextView> TextViewMock(string contentType)
        {
            Mock<ITextView> textView = new Mock<ITextView>();
            textView.Setup(m => m.TextSnapshot.Length)
                    .Returns(() => _mockedScript.Length);
            textView.Setup(m => m.Caret)
                    .Returns(() =>
                                {
                                    var textSnapshot = new Mock<ITextSnapshot>();
                                    textSnapshot.Setup(m => m.Length)
                                                .Returns(_mockedScript.Length);
                                    var snapshotPoint = new SnapshotPoint(textSnapshot.Object, _mockedCaret);
                                    var virtualSnapshotPoint = new VirtualSnapshotPoint(snapshotPoint);
                                    var caretPosition = new CaretPosition(virtualSnapshotPoint, (new Mock<IMappingPoint>()).Object, PositionAffinity.Successor);

                                    Mock<ITextCaret> textCaret = new Mock<ITextCaret>();
                                    textCaret.SetupGet(m => m.Position)
                                             .Returns(caretPosition);
                                    return textCaret.Object;
                                });
            textView.Setup(m => m.TextBuffer.ContentType.TypeName)
                    .Returns(() => contentType);
            textView.Setup(m => m.TextSnapshot.CreateTrackingPoint(It.IsAny<int>(), It.IsAny<PointTrackingMode>()))
                    .Returns<int, PointTrackingMode>((p, q) =>
                                                            {
                                                                Mock<ITrackingPoint> trackingPoint = new Mock<ITrackingPoint>();
                                                                trackingPoint.Setup<char>(m => m.GetCharacter(It.IsAny<ITextSnapshot>()))
                                                                             .Returns(() => _mockedScript[q == PointTrackingMode.Positive ? p : p - 1]);
                                                                return trackingPoint.Object;
                                                            });

            return textView;
        }

        private Mock<IEditorOperations> EditorOperationsMock()
        {
            Mock<Microsoft.VisualStudio.Text.Operations.IEditorOperations> editorOperations = new Mock<IEditorOperations>();
            editorOperations.Setup(m => m.AddBeforeTextBufferChangePrimitive())
                            .Callback(() => { });
            editorOperations.Setup(m => m.AddAfterTextBufferChangePrimitive())
                            .Callback(() => { });

            // {|} --> {p|}
            editorOperations.Setup(m => m.InsertText(It.IsAny<string>()))
                            .Callback<string>(p =>
                                                {
                                                    _mockedScript = _mockedScript.Insert(_mockedCaret, p);
                                                    _mockedCaret++;
                                                });

            // {|} --> {|
            editorOperations.Setup(m => m.Delete())
                            .Callback(() =>
                                        {
                                            _mockedScript = _mockedScript.Remove(_mockedCaret, 1);
                                        });

            // {}| --> {|}  
            editorOperations.Setup(m => m.MoveToPreviousCharacter(false))
                            .Callback(() =>
                                        {
                                            _mockedCaret--;
                                        });

            // {|} --> {}|
            editorOperations.Setup(m => m.MoveToNextCharacter(false))
                            .Callback(() =>
                                        {
                                            _mockedCaret++;
                                        });

            // {|} --> {\r\n|}
            editorOperations.Setup(m => m.InsertNewLine())
                            .Callback(() =>
                                        {
                                            _mockedScript = _mockedScript.Insert(_mockedCaret, Environment.NewLine);
                                            _mockedCaret += Environment.NewLine.Length;
                                        });

            // {\r\n|} --> {|\r\n}
            editorOperations.Setup(m => m.MoveLineUp(false))
                            .Callback(() =>
                                        {
                                            _mockedCaret = _mockedScript.Substring(0, _mockedCaret).LastIndexOf(Environment.NewLine);
                                        });

            editorOperations.Setup(m => m.MoveToEndOfLine(false))
                            .Callback(() =>
                                        {
                                            // Should move caret to the end, here we remain it still as it's already at the end.
                                        });

            // {\r\n|\r\n} --> {\r\n  |\r\n}
            editorOperations.Setup(m => m.Indent())
                            .Callback(() =>
                                        {
                                            const string fakedIndent = "  ";
                                            _mockedScript = _mockedScript.Insert(_mockedCaret, fakedIndent);
                                            _mockedCaret += fakedIndent.Length;
                                        });
            return editorOperations;
        }

        private Mock<ITextUndoHistory> TextUndoHistoryMock()
        {
            Mock<ITextUndoHistory> textUndoHistory = new Mock<ITextUndoHistory>();
            textUndoHistory.Setup(m => m.CreateTransaction(It.IsAny<string>()))
                           .Returns(() =>
                                        {
                                            Mock<ITextUndoTransaction> textUndoTransactioin = new Mock<ITextUndoTransaction>();
                                            textUndoTransactioin.Setup(m => m.Complete()).Callback(() => { });
                                            return textUndoTransactioin.Object;
                                        });
            textUndoHistory.Setup(m => m.Undo(It.IsAny<int>()))
                           .Callback<int>(p =>
                                            {
                                                _mockedScript = _mockedScript.Remove(--_mockedCaret, 2);
                                            });
            return textUndoHistory;
        }
    }
}
