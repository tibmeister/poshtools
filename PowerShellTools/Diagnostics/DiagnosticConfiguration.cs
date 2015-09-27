using PowerShellTools.Common.Logging;

namespace PowerShellTools.Diagnostics
{
    class DiagnosticConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DiagnosticConfiguration));

        public static void DisableDiagnostics()
        {
            Log.Info("Diagnostics disabled.");
            LogManager.SetLoggingLevel("OFF");
        }

        public static void EnableDiagnostics()
        {
            LogManager.SetLoggingLevel("ALL");
            Log.Info("Diagnostics enabled.");
        }


    }
}
