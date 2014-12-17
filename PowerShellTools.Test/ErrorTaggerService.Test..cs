using System.Linq;
using System.Management.Automation.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using PowerShellTools.Classification;

namespace PowerShellTools.Test
{
    [TestClass]
    public class ErrorTaggerServiceTest
    {
        private ErrorTagSpanService _service;
        private Mock<ITextBuffer> _buffer;
        private Mock<ITextSnapshot> _bufferCurrentSnapshot;
        private Mock<ITrackingSpan> _spanToTokenize;
        private PropertyCollection _propertyCollection;

        [TestInitialize]
        public void Init()
        {
            _service = new ErrorTagSpanService();

            _buffer = new Mock<ITextBuffer>();
            _bufferCurrentSnapshot = new Mock<ITextSnapshot>();
            _propertyCollection = new PropertyCollection();
            _spanToTokenize = new Mock<ITrackingSpan>();

            _bufferCurrentSnapshot.Setup(
                m => m.CreateTrackingSpan(It.IsAny<int>(), It.IsAny<int>(), SpanTrackingMode.EdgeInclusive)).Returns(_spanToTokenize.Object);

            _buffer.SetupGet(m => m.Properties).Returns(_propertyCollection);
            _buffer.SetupGet(m => m.CurrentSnapshot).Returns(_bufferCurrentSnapshot.Object);
        }

        [TestMethod]
        public void ShouldTagUnclosedStringErrorSpan()
        {
            var script = @"
                'This is a string with no closure
            ";

            _bufferCurrentSnapshot.Setup(m => m.Length).Returns(script.Length);
            _spanToTokenize.Setup(m => m.GetText(_bufferCurrentSnapshot.Object)).Returns(script);

            Token[] tokens;
            ParseError[] errors;
            Parser.ParseInput(script, out tokens, out errors);

            var errorSpans = _service.TagErrorSpans(_buffer.Object, 0, errors);
           
            var errorTag = errorSpans.First().GetTagSpan(_bufferCurrentSnapshot.Object);

            Assert.AreEqual(errorTag.Tag.ErrorType, "syntax error");
            Assert.AreEqual(errorTag.Tag.ToolTipContent, "The string is missing the terminator: '.");
        }

    }
}
