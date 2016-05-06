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

        public void Start()
        {
            foreach (WatcherInfo watcherInfo in (List<WatcherInfo>)ConfigurationManager.GetSection("watchers")) {
                try {
                    FileWatcherBase watcher = watcherInfo.CreateWatcher();
                    watcher.Init();
                    _watchers.Add(watcher);
                } catch (Exception ex) {
                    _log.Error(string.Format("Could not start watcher {0}", watcherInfo != null ? watcherInfo.Name : "watcherInfo was null"), ex);
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