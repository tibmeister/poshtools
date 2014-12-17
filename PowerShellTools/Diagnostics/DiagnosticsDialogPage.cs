using System.ComponentModel;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace PowerShellTools.Diagnostics
{
    class DiagnosticsDialogPage : DialogPage
    {
        private bool _enableDiagLogging;

        public DiagnosticsDialogPage()
        {
            var cm = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _events = cm.GetService<VisualStudioEvents>();
        }

        private readonly VisualStudioEvents _events;

        [DisplayName(@"Enable Diagnostic Logging")]
        [Description("Log messages will be written to the output pane.")]
        public bool EnableDiagnosticLogging
        {
            get { return _enableDiagLogging; }
            set
            {
                _enableDiagLogging = value;
                _events.OnSettingsChanged(this);
            }
        }
    }
}
