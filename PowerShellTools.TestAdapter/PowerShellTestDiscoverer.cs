using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace PowerShellTools.TestAdapter
{
    [DefaultExecutorUri(PowerShellTestExecutor.ExecutorUriString)]
    [FileExtension(".ps1")]
    public class PowerShellTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            GetTests(sources, discoverySink, logger);
        }

        public static List<TestCase> GetTests(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink, IMessageLogger logger = null)
        {
            var tests = new List<TestCase>();
            foreach (var source in sources)
            {
                var testType = TestType.Pester;

                var scriptContents = System.IO.File.ReadAllText(source);
                if (scriptContents.StartsWith("#pester", StringComparison.OrdinalIgnoreCase))
                {
                    testType = TestType.Pester;
                }
                else if (scriptContents.StartsWith("#psate", StringComparison.OrdinalIgnoreCase))
                {
                    testType = TestType.PSate;
                }

                if (testType == TestType.Pester)
                {
                    DiscoverPesterTests(discoverySink, logger, source, tests);    
                }
                else if (testType == TestType.PSate)
                {
                    DiscoverPsateTests(discoverySink, source, tests);    
                }
            }
            return tests;
        }

        private static void DiscoverPesterTests(ITestCaseDiscoverySink discoverySink, IMessageLogger logger, string source,
            List<TestCase> tests)
        {
            SendMessage(TestMessageLevel.Informational, String.Format("Searching for tests in [{0}].", source), logger);
            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseFile(source, out tokens, out errors);

            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    SendMessage(TestMessageLevel.Error, String.Format("Parser error. {0}", error.Message), logger);
                }
                return;
            }

            var testSuites =
                ast.FindAll(
                    m =>
                        (m is CommandAst) &&
                        (m as CommandAst).GetCommandName().Equals("describe", StringComparison.OrdinalIgnoreCase), true);


            foreach (var ast1 in testSuites)
            {
                var describeName = GetFunctionName(logger, ast1, "describe");

                var contextes = ast1.FindAll(
                    m =>
                        (m is CommandAst) &&
                        (m as CommandAst).GetCommandName().Equals("context", StringComparison.OrdinalIgnoreCase), true);

                foreach(var context in contextes)
                {
                    var contextName = GetFunctionName(logger, context, "context");

                    var its = context.FindAll(
                    m =>
                        (m is CommandAst) &&
                        (m as CommandAst).GetCommandName().Equals("it", StringComparison.OrdinalIgnoreCase), true);

                    foreach (var test in its)
                    {
                        var itAst = (CommandAst)test;
                        var itName = GetFunctionName(logger, test, "it");

                        // Didn't find the name for the test. Skip it.
                        if (String.IsNullOrEmpty(itName))
                        {
                            SendMessage(TestMessageLevel.Informational, "Test name was empty. Skipping test.", logger);
                            continue;
                        }

                        var displayName = String.Format("{0} It {1}", contextName, itName);
                        var fullName = String.Format("{0}||{1}||{2}||{3}", "Pester", describeName, contextName, itName);

                        var testcase = new TestCase(fullName, PowerShellTestExecutor.ExecutorUri, source)
                        {
                            DisplayName = displayName,
                            CodeFilePath = source,
                            LineNumber = itAst.Extent.StartLineNumber
                        };

                        SendMessage(TestMessageLevel.Informational,
                            String.Format("Adding test [{0}] in {1} at {2}.", displayName, source, testcase.LineNumber), logger);

                        if (discoverySink != null)
                        {
                            SendMessage(TestMessageLevel.Informational, "Sending test to sync.", logger);
                            discoverySink.SendTestCase(testcase);
                        }

                        tests.Add(testcase);
                    }
                }
            }
        }

        private static string GetFunctionName(IMessageLogger logger, Ast context, string functionName)
        {
            var contextAst = (CommandAst) context;
            SendMessage(TestMessageLevel.Informational, String.Format("Found {0} block.", functionName), logger);
            var contextName = String.Empty;
            bool nextElementIsName1 = false;
            foreach (var element in contextAst.CommandElements)
            {
                if (element is StringConstantExpressionAst &&
                    !(element as StringConstantExpressionAst).Value.Equals(functionName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    contextName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (nextElementIsName1 && element is StringConstantExpressionAst)
                {
                    contextName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (element is CommandParameterAst &&
                    (element as CommandParameterAst).ParameterName.Equals("Name",
                        StringComparison.OrdinalIgnoreCase))
                {
                    nextElementIsName1 = true;
                }
            }

            return contextName;
        }

        private static void DiscoverPsateTests(ITestCaseDiscoverySink discoverySink, string source, List<TestCase> tests)
        {
            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseFile(source, out tokens, out errors);

            var testAsts = ast.FindAll(m => (m is CommandAst) && (m as CommandAst).GetCommandName().Equals("TestFixture", StringComparison.OrdinalIgnoreCase), true);

            foreach (var ast1 in testAsts)
            {
                var testFixtureAst = (CommandAst) ast1;
                var testCaseAsts =
                    testFixtureAst.FindAll(m => (m is CommandAst) && (m as CommandAst).GetCommandName().Equals("TestCase", StringComparison.OrdinalIgnoreCase), true);

                try
                {
                    var textFixtureName = GetTestFixtureName(testFixtureAst);

                    foreach (var ast2 in testCaseAsts)
                    {
                        var contextAst = (CommandAst) ast2;
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
                    
                }
            }
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
                    contextName = String.Format("PSate||{0}||{1}", textFixtureName, (element as StringConstantExpressionAst).Value);
                    displayName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (nextElementIsName && element is StringConstantExpressionAst)
                {
                    contextName = String.Format("PSate||{0}||{1}", textFixtureName, (element as StringConstantExpressionAst).Value);
                    displayName = (element as StringConstantExpressionAst).Value;
                    break;
                }

                if (element is CommandParameterAst &&
                    (element as CommandParameterAst).ParameterName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    nextElementIsName = true;
                }
            }

            var testcase = new TestCase(contextName, PowerShellTestExecutor.ExecutorUri, source)
            {
                CodeFilePath = source,
                DisplayName = displayName,
                LineNumber = contextAst.Extent.StartLineNumber,
                LocalExtensionData = "PSate"
            };

            return testcase;
        }

        private static void SendMessage(TestMessageLevel level, string message, IMessageLogger logger)
        {
            if (logger != null)
            {
                logger.SendMessage(level, message);
            }
        }
    }
}