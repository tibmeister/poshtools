using System.Management.Automation.Language;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Moq;
using PowerShellTools.Classification;
using PowerShellTools.Intellisense;

namespace PowerShellTools.Test.IntelliSense
{
    [TestClass]
    public class CommentAreaUnitTests
    {
        [TestMethod]
        public void TestEmptyScript()
        {
            IsInCommentAreaTestHelper("", 0, false);
        }

        [TestMethod]
        public void TestNoCommentScript()
        {
            string script = @"param(
                                [parameter(Mandatory=$true)]
                                [string]$someStr
                                )";
            IsInCommentAreaTestHelper(script, 0, false);
            IsInCommentAreaTestHelper(script, 10, false);
            IsInCommentAreaTestHelper(script, script.Length, false);
        }

        [TestMethod]
        public void TestAllLineCommentScript()
        {
            string script = @"#param(
                              #  [parameter(Mandatory=$true)]
                              #  [string]$someStr
                              #  )";
            IsInCommentAreaTestHelper(script, 0, true);
            IsInCommentAreaTestHelper(script, 1, true);
            IsInCommentAreaTestHelper(script, 50, true);
            IsInCommentAreaTestHelper(script, script.Length, true);
        }

        [TestMethod]
        public void TestAllBlockCommentScript()
        {
            string script = @"<# param(
                                 [parameter(Mandatory=$true)]
                                 [string]$someStr
                                 )
                              #>";
            IsInCommentAreaTestHelper(script, 0, true);
            IsInCommentAreaTestHelper(script, 20, true);
            IsInCommentAreaTestHelper(script, script.Length, true);
        }

        [TestMethod]
        public void TestNormalCommentScript()
        {
            string script = @"<# This is a block comment
                              #>
                              param(
                                [parameter(Mandatory=$true)]
                                [string]$someStr
                                )
                              # This is line comment 1
                              # This is line comment 2";
            IsInCommentAreaTestHelper(script, 0, true);
            IsInCommentAreaTestHelper(script, 10, true);
            IsInCommentAreaTestHelper(script, 100, false);
            IsInCommentAreaTestHelper(script, 300, true);
            IsInCommentAreaTestHelper(script, script.Length, true);
        }

        private void IsInCommentAreaTestHelper(string script, int caretPosition, bool expected)
        {
            Token[] tokens;
            ParseError[] errors;
            Parser.ParseInput(script, out tokens, out errors);

            Mock<ITextBuffer> textBuffer = new Mock<ITextBuffer>();
            TextBufferMockHelper(textBuffer, script, tokens);
            bool actual = Utilities.IsInCommentArea(caretPosition, textBuffer.Object);

            Assert.AreEqual(expected, actual);
        }

        private void TextBufferMockHelper(Mock<ITextBuffer> textBuffer, string script, Token[] tokens)
        {
            PropertyCollection pc = new PropertyCollection();
            pc.AddProperty(BufferProperties.Tokens, tokens);
            textBuffer.Setup(m => m.Properties).Returns(pc);
            textBuffer.Setup(m => m.CurrentSnapshot.Length)
                      .Returns(() => script.Length);
        }
    }
}
