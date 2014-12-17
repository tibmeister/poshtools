using System.Linq;
using System.Management.Automation.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Classification;
using Moq;
using PowerShellTools.Classification;

namespace PowerShellTools.Test
{
    [TestClass]
    public class ClassifierServiceTest
    {
        private ClassifierService _classifierService;
        private Mock<IClassificationTypeRegistryService> _classificationRegistry;
        private Mock<IClassificationType> _variableType;
        private Mock<IClassificationType> _stringType;

        [TestInitialize]
        public void Init()
        {
            _classificationRegistry = new Mock<IClassificationTypeRegistryService>();

            _variableType = new Mock<IClassificationType>();
            _variableType.Setup(m => m.Classification).Returns("variable");
            _classificationRegistry.Setup(m => m.GetClassificationType(Classifications.PowerShellVariable)).Returns(_variableType.Object);

            _stringType = new Mock<IClassificationType>();
            _stringType.Setup(m => m.Classification).Returns("string");
            _classificationRegistry.Setup(m => m.GetClassificationType(Classifications.PowerShellString)).Returns(_stringType.Object);
            
            EditorImports.ClassificationTypeRegistryService = _classificationRegistry.Object;
            _classifierService = new ClassifierService();
        }

        [TestMethod]
        public void ShouldClassifyVariable()
        {
            var script = "$variable";

            Token[] tokens;
            ParseError[] errors;
            Parser.ParseInput(script, out tokens, out errors);



            var infos = _classifierService.ClassifyTokens(tokens, 0);

            Assert.AreEqual("variable", infos.ElementAt(0).ClassificationType.Classification);
        }

        [TestMethod]
        public void ShouldClassifyExpandableString()
        {
            var script = "\"$variable in a string\"";

            Token[] tokens;
            ParseError[] errors;
            Parser.ParseInput(script, out tokens, out errors);

            var infos = _classifierService.ClassifyTokens(tokens, 0);

            Assert.AreEqual("string", infos.ElementAt(0).ClassificationType.Classification);
            Assert.AreEqual("variable", infos.ElementAt(1).ClassificationType.Classification);
        }
    }
}
