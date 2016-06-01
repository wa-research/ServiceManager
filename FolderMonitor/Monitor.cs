using System;
using System.Collections.Generic;
using System.Configuration;

namespace FolderMonitor
{
    public class Monitor
    {
        protected static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Monitor));

        List<FileWatcherBase> _watchers = new List<FileWatcherBase>();

        public List<FileWatcherBase> ConfiguredWatchers { get { return _watchers; } }

        public FileWatcherBase CreateWatcher(WatcherInfo info)
        {
            Type type = Type.GetType(info.WatcherType);
            return (FileWatcherBase)Activator.CreateInstance(type, info);
        }

        public void Start()
        {
            var configuredWatchers = (List<WatcherInfo>)ConfigurationManager.GetSection("watchers");
            foreach (WatcherInfo watcherInfo in configuredWatchers) {
                try {
                    FileWatcherBase watcher = CreateWatcher(watcherInfo);
                    watcher.Init();
                    _watchers.Add(watcher);
                } catch (Exception ex) {
                    _log.Error(string.Format("Could not start watcher {0}", watcherInfo != null ? watcherInfo.Name : "watcherInfo was null"), ex);
                    Console.WriteLine(string.Format("Could not start watcher {0}: {1} {2}", watcherInfo != null ? watcherInfo.Name : "watcherInfo was null", ex.Message, ex.StackTrace), ex);
                }
            }
        }

        public void Stop()
        {
            foreach (FileWatcherBase watcher in _watchers) {
                watcher.Dispose();
            }
        }
    }
}