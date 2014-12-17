using System;
using System.IO;
using log4net.Appender;
using log4net.Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PowerShellTools.Diagnostics
{
    internal class OutputPaneAppender : AppenderSkeleton
    {
        private readonly IVsOutputWindowPane _outputPane;
        private readonly Guid _debugPaneGuid = new Guid("710A66CF-2D26-410C-BBFA-1110BB03D40D");

        public OutputPaneAppender()
        {
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow != null)
            {
                int hr = outputWindow.CreatePane(_debugPaneGuid, "PowerShell Tools Debug", 1, 0);
                hr = outputWindow.GetPane(_debugPaneGuid, out _outputPane);
                _outputPane.Activate();
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var writer = new StringWriter();
            Layout.Format(writer, loggingEvent);

            var message = writer.GetStringBuilder().ToString();
            _outputPane.OutputStringThreadSafe(message);
        }
    }
}
