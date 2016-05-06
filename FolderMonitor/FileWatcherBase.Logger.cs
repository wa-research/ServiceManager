using System;

namespace FolderMonitor
{
    public partial class FileWatcherBase
    {
        public class Logger
        {
            protected static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(FileWatcherBase));

            string _watcherName;

            public Logger(string watcherName)
            {
                _watcherName = watcherName;
            }

            public void Info(string message, params object[] args)
            {
                _log.InfoFormat(_watcherName + ": " + message, args);
            }

            public void Debug(string message, params object[] args)
            {
                _log.DebugFormat(_watcherName + ": " + message, args);
            }

            public void Error(Exception ex)
            {
                _log.Error(_watcherName, ex);
            }

            public void Error(string message, params object[] args)
            {
                _log.ErrorFormat(_watcherName + ": " + message, args);
            }

        }
    }
}
