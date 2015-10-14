using System;

namespace PowerShellTools.Common.Logging
{
    public class Log : ILog
    {
        private readonly log4net.ILog _log;

        public Log(log4net.ILog log)
        {
            _log = log;
        }

        public void Error(string message)
        {
            _log.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            _log.Error(message, exception);
        }

        public void ErrorFormat(string format, params object[] param)
        {
            _log.ErrorFormat(format, param);
        }

        public void Info(string message)
        {
            _log.Info(message);
        }

        public void InfoFormat(string format, params object[] param)
        {
            _log.InfoFormat(format, param);
        }

        public void Debug(string message)
        {
            _log.Debug(message);
        }

        public void DebugFormat(string format, params object[] param)
        {
            _log.DebugFormat(format, param);
        }

        public void Debug(string message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        public void Warn(string message, Exception exception)
        {
            _log.Warn(message, exception);
        }

        public void Warn(string message)
        {
            _log.Warn(message);
        }

        public void WarnFormat(string format, params object[] param)
        {
            _log.WarnFormat(format, param);
        }
    }
}
