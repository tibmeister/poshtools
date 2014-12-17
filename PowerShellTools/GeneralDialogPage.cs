using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace PowerShellTools
{
    class GeneralDialogPage : DialogPage
    {
        [DisplayName(@"Override Execution Policy Configuration")]
        [Description("Overrides the behavior that is defined for the machine or user. This enables PowerShell Tools to run scripts without modifying the registry. Chaning this setting requries a restart of Visual Studio.")]
        public bool OverrideExecutionPolicyConfiguration { get; set; }
        [DisplayName(@"Multiline REPL Window")]
        [Description("When false, pressing enter invokes the command line rather than starting a new line.")]
        public bool MutlilineRepl { get; set; }
    }
}
