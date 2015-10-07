using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace PowerShellTools.TestAdapter
{
    public class PowerShellTestResult
    {
        public PowerShellTestResult(TestOutcome outcome)
        {
            Outcome = outcome;
        }

        public PowerShellTestResult(TestOutcome outcome, string errorMessage, string errorStacktrace)
        {
            Outcome = outcome;
            ErrorMessage = errorMessage;
            ErrorStacktrace = errorStacktrace;
        }

        public TestOutcome Outcome { get; private set; }
        public string ErrorMessage { get; private set; }
        public string ErrorStacktrace { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}\n{1}\n{2}", Outcome, ErrorMessage, ErrorStacktrace);
        }
    }
}
