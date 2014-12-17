using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PowerShellTools.TestAdapter
{
    [DefaultExecutorUri(PsateTestExecutor.ExecutorUriString)]
    [FileExtension(".ps1")]
    public class PsateTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            GetTests(sources, discoverySink);
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            var tests = new List<TestCase>();
            foreach (var source in sources)
            {
                Token[] tokens;
                ParseError[] errors;
                var ast = Parser.ParseFile(source, out tokens, out errors);

                var testAsts = ast.FindAll(m => (m is CommandAst) && (m as CommandAst).GetCommandName() == "TestFixture", true);

                foreach (CommandAst testFixtureAst in testAsts)
                {
                    var testCaseAsts = testFixtureAst.FindAll(m => (m is CommandAst) && (m as CommandAst).GetCommandName() == "TestCase", true);

                    try
                    {
                        var textFixtureName = GetTestFixtureName(testFixtureAst);

                        foreach (CommandAst contextAst in testCaseAsts)
                        {
                            var testcase = GetTestCase(contextAst, textFixtureName, source);


                            if (discoverySink != null)
                            {
                                discoverySink.SendTestCase(testcase);
                            }

                            tests.Add(testcase);
                        }
                    }
                    catch
                    {
                        continue;
                    }

                }
            }
            return tests;
        }

        private static string GetTestFixtureName(CommandAst testFixtureAst)
        {
            bool nextElementIsName = false;
            foreach (var element in testFixtureAst.CommandElements)
            {
                if (
                    element is StringConstantExpressionAst &&
                    !(element as StringConstantExpressionAst).Value.Equals("TestFixture", StringComparison.OrdinalIgnoreCase) &&
                    !(element as StringConstantExpressionAst).Value.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    return (element as StringConstantExpressionAst).Value;        
                }

                if (nextElementIsName && element is StringConstantExpressionAst)
                {
                    return (element as StringConstantExpressionAst).Value;
                }

                if (element is CommandParameterAst &&
                    (element as CommandParameterAst).ParameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    nextElementIsName = true;
                }
            }

            throw new Exception("Failed to find test fixture name!");
        }

        private static TestCase GetTestCase(CommandAst contextAst, string textFixtureName, string source)
        {
            var contextName = String.Empty;
            var displayName = String.Empty;
            bool nextElementIsName = false;
            foreach (var element in contextAst.CommandElements)
            {
                if (
                    element is StringConstantExpressionAst &&
                    !(element as StringConstantExpressionAst).Value.Equals("TestCase", StringComparison.OrdinalIgnoreCase) &&
                    !(element as StringConstantExpressionAst).Value.Equals("Name", StringComparison.OrdinalIgnoreCase) &&
                    !(element as StringConstantExpressionAst).Value.Equals("ScriptBlock", StringComparison.OrdinalIgnoreCase))
                {
                    contextName = String.Format("{0},{1}", textFixtureName, (element as StringConstantExpressionAst).Value);
                    displayName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (nextElementIsName && element is StringConstantExpressionAst)
                {
                    contextName = String.Format("{0},{1}", textFixtureName, (element as StringConstantExpressionAst).Value);
                    displayName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (element is CommandParameterAst &&
                    (element as CommandParameterAst).ParameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    nextElementIsName = true;
                }
            }

            var testcase = new TestCase(contextName, PsateTestExecutor.ExecutorUri, source)
            {
                CodeFilePath = source,
                DisplayName = displayName,
                LineNumber = contextAst.Extent.StartLineNumber
            };

            return testcase;
        }
    }
}