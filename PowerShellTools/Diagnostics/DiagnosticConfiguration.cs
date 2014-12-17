using log4net;
using log4net.Config;
using log4net.Layout;

namespace PowerShellTools.Diagnostics
{
    class DiagnosticConfiguration
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (DiagnosticConfiguration));

        public static void DisableDiagnostics()
        {
            Log.Info("Diagnostics disabled.");
            SetLoggingLevel("OFF");
        }

        public static void EnableDiagnostics()
        {
            var appender = new OutputPaneAppender();
            appender.Layout = new PatternLayout("%date - %thread - %logger - %level - %message%newline");
            appender.ActivateOptions();

            BasicConfigurator.Configure(appender);

            SetLoggingLevel("ALL");

            Log.Info("Diagnostics enabled.");
        }

        private static void SetLoggingLevel(string level)
        {
            log4net.Repository.ILoggerRepository[] repositories = log4net.LogManager.GetAllRepositories();

            //Configure all loggers to be at the debug level.
            foreach (log4net.Repository.ILoggerRepository repository in repositories)
            {
                repository.Threshold = repository.LevelMap[level];
                log4net.Repository.Hierarchy.Hierarchy hier = (log4net.Repository.Hierarchy.Hierarchy)repository;
                log4net.Core.ILogger[] loggers = hier.GetCurrentLoggers();
                foreach (log4net.Core.ILogger logger in loggers)
                {
                    ((log4net.Repository.Hierarchy.Logger)logger).Level = hier.LevelMap[level];
                }
            }

            //Configure the root logger.
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
            rootLogger.Level = h.LevelMap[level];
        }
    }
}
