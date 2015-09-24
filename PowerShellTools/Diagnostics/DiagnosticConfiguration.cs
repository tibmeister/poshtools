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

        private static void EnsureDiagnosticsInitialized()
        {
            LogManager.Initialize();
        }

        public static void EnableDiagnostics()
        {
            EnsureDiagnosticsInitialized();

            LogManager.SetLoggingLevel("ALL");

            Log.Info("Diagnostics enabled.");
        }


    }
}
