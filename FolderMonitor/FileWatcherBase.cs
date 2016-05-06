using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FindFiles;

namespace FolderMonitor
{
    public partial class FileWatcherBase : IDisposable
    {
        #region Class variables
        private static object _locker = new object();
        private static HashSet<string> _fileLock = new HashSet<string>();
        private Timer _periodicScanTimer;
#if TRACE_QUEUE_SIZE
        private Timer _batchMonitorTimer;
#endif
        protected FileSystemWatcher _watcher = null;
        protected WatcherInfo _info;
        protected string _uri;
        protected string _inputFolder;
        protected string _archiveFolder;
        protected string _deletedFolder;
        protected string _outputFolder;
        protected string _errorFolder;
        protected Logger Log;
        protected ProducerConsumerQueue _queue;
        #endregion

        #region Constructor
        public FileWatcherBase(WatcherInfo info)
        {
            _info = info;
            _queue = new ProducerConsumerQueue(info.Threads == 0 ? 4 : info.Threads);
        }
        #endregion

        #region Initialization
        public virtual void Init()
        {
            Log = new Logger(_info.Name ?? _info.WatcherType.Substring(_info.WatcherType.LastIndexOf('.') + 1));

            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            //Input folder must be specified - either absolute, or relative
            //(in which case it becomes a subfolder of the AppDomain current folder)
            //Input must exist -- we do this to avoid creating random folders on disk
            //if there's a typo in the configuration
            _inputFolder = MakeRootedPath(basePath, _info.InputFolder);

            //All other folders are children of input folder by default
            //If a folder is overriden in a config file, do any of
            // a) If override is rooted, use that explicit path
            // b) Else append to input folder
            if (!_info.NoDefaultFolders) {
                _archiveFolder = SetupConfiguredFolder(_inputFolder, _info.ArchiveFolder, "processed");
                if (!_info.DeleteMeansDelete)
                    _deletedFolder = SetupConfiguredFolder(_inputFolder, _info.DeletedFolder, "deleted");
                _errorFolder = SetupConfiguredFolder(_inputFolder, _info.ErrorFolder, "error");
            }

            //Some processors want a separate output folder to output transformed files
            //(as opposed to 'Processed' folder that archives unchanged input files after they are processed)
            _outputFolder = MakeRootedPath(basePath, _info.OutputFolder);
            //Some processors will use an URI for output
            _uri = _info.Url;

            if (_info.WatchSubfolders && (IsSubFolderOf(_inputFolder, _archiveFolder) || IsSubFolderOf(_inputFolder, _deletedFolder) || IsSubFolderOf(_inputFolder, _errorFolder))) {
                _info.WatchSubfolders = false;
                Log.Error("Will not watch subfolders as some of the configured folders are subfolders of input folder");
            }
            if (_info.NoDefaultFolders) {
                Log.Info("Registering watcher '{0}': input from '{1}{2}', leave files in place.", _info.WatcherType, _inputFolder, _info.WatchSubfolders ? "\\*" + _info.Filter : "");
            } else {
                Log.Info("Registering watcher '{0}': input from '{1}{3}', archive to '{2}'", _info.WatcherType, _inputFolder, _archiveFolder, _info.WatchSubfolders ? "\\*" + _info.Filter : "");
            }
            if (!Directory.Exists(_inputFolder)) {
                Log.Error("Input folder {0} does not exist. Please create the folder and restart this service.", _inputFolder);
            }
            if (!string.IsNullOrWhiteSpace(_outputFolder)) {
                Log.Info("Transformed output will go into '{0}'", _outputFolder);
            }

            _watcher = new FileSystemWatcher(_inputFolder);
            _watcher.Created += new FileSystemEventHandler(FileCreated);
            if (!string.IsNullOrEmpty(_info.Filter)) {
                _watcher.Filter = _info.Filter;
            }
            _watcher.IncludeSubdirectories = _info.WatchSubfolders;
            _watcher.EnableRaisingEvents = true;

            SetUpTimers();
        }

        private void SetUpTimers()
        {
            //Run one pass through folder after startup. Start with random delay of 5-20 seconds to avoid 
            //killing the process immediately after start if there are many files
            int firstInterval = new Random().Next(5, 20);
            Timer firstTimeScan = new Timer(o => ProcessFiles("Initial scan."), null, firstInterval * 1000, Timeout.Infinite);
            Log.Info("Initial folder scan to clean up any existing files will happen in {0} seconds.", firstInterval);

            //Watch for any unprocessed files at a configured interval
            int interval = _info.CleanupInterval * 1000;
            if (interval > 0) {
                _periodicScanTimer = new Timer(i => TimerElapsed(i), interval, interval, interval);
                Log.Info("Regular clean-up scans will occur every {0} seconds.", _info.CleanupInterval);
            } else {
                Log.Info("Regular clean-up scans are disabled.");
            }
#if TRACE_QUEUE_SIZE
            _batchMonitorTimer = new Timer(ReportQueueLength, null, firstInterval * 1000, 200);
#endif
        }
        #endregion

        #region Queue monitor
        private static List<int> _queueLengths;
        private static long _lastQueueReport = DateTime.Now.Ticks;

        void ReportQueueLength(object t)
        {
            var now = DateTime.Now.Ticks;
            lock (_locker) {
                _queueLengths = _queueLengths ?? new List<int>();
                _queueLengths.Add(_queue.Length);
                var delta = now - _lastQueueReport;
                //1 million ticks = 0.1 second
                if (delta > 100 * 1000 * 1000) {
                    _lastQueueReport = now;
                    Log.Debug("Last {0} queue lengths: {1} [Delta: {2}]", _queueLengths.Count, string.Join(",", _queueLengths.Select(i => i.ToString()).ToArray()), delta / 10000000F);
                    _queueLengths = new List<int>();
                }
            }
        }
        #endregion

        #region Batch flag
        private static bool _inBatch;

        private void StartBatch()
        {
            lock (_locker) {
                if (!_inBatch) {
                    _inBatch = true;
                }
            }
        }

        private void StopBatch()
        {
            lock (_locker) {
                _inBatch = false;
            }
        }
        #endregion

        void FileCreated(object sender, FileSystemEventArgs e)
        {
            try {
                EnqueueFile(e.FullPath, e.Name);
            } catch (Exception ex) {
                Log.Error(ex);
            }
        }

        private bool EnqueueFile(string fullPath, string name)
        {
            var successfullyLocked = LockPath(fullPath);
            if (successfullyLocked) {
                _queue.EnqueueTask(() => HandleFilePrivate(fullPath, name));
            } else {
                Log.Error("Tried to enqueue already seen file: {0}", fullPath);
            }
            return successfullyLocked;
        }

        void HandleFilePrivate(string path, string name)
        {
            //Do locking here and only then call the overriden HandleFile
            try {
                if (File.Exists(path)) {
                    if (TryObtainExclusiveFileLock(path)) {
                        HandleFile(path, name);
                        //Log.Debug("Queue is now at {0}", _queue.Length);
                    } else {
                        //Let the periodic cleaner pick it up
                        UnlockPath(path);
                        if (_info.CleanupInterval == 0) {
                            //Cleaner is off, push the task at the end of the queue 
                            Thread.Sleep(1000);
                            Log.Debug("Re-queueing file {0}", path);
                            EnqueueFile(path, name);
                        }
                    }
                } else {
                    Log.Debug("The file {0} no longer exists.", path);
                }
            } catch (Exception ex) {
                //The file stays in the queue for periodic scan to clean it up.
                UnlockPath(path);
                Log.Error("Failed to handle path {0}: {1}", path, ex);
            } 
        }

        protected virtual void HandleFile(string path, string name)
        {
            if (IsLocked(path)) {
                Log.Debug("Tried to process locked file: {0}", path);
                return;
            } else {
                Log.Debug("Processing file: {0}", path);
            }
        }

        #region Periodic and initial scan
        void TimerElapsed(object interval)
        {
            lock (_locker) if (_inBatch) return;
            try {
                _periodicScanTimer.Change(Timeout.Infinite, Timeout.Infinite);
                ProcessFiles("Periodic folder scan");
                _periodicScanTimer.Change((int)interval, (int)interval);
            } catch (Exception ex) {
                Log.Error("Exception handling files. ERROR: {0}", ex.ToString().Replace("\n", " "));
            }
        }

        void ProcessFiles(string msg)
        {
            lock (_locker) if (_inBatch) return;
            //Open the folder and keep going until there are any files left
            //Skip any locked files
            try {
                if (LockPath(_inputFolder)) {
                    Log.Debug("{0} ({1}\\{2})", msg, _inputFolder, _info.Filter);
                    StartBatch();
                    FileSystemEnumerator fse = new FileSystemEnumerator(_inputFolder, _info.Filter ?? "*", includeSubDirs: false);
                    int i = 0;
                    foreach (FileInfo f in fse.Matches()) {
                        if (!IsLocked(f.FullName)) {
                            EnqueueFile(f.FullName, f.Name);
                            ++i;
                        }
                    }
                    Log.Debug("{0} - enqueued {1} files", msg, i);
                    StopBatch();
                    UnlockPath(_inputFolder);
                }
            } catch (Exception e) {
                Log.Error("Exception processing files. ERROR: {0}", e.ToString().Replace("\n", " "));
            }
        }
        #endregion

        #region Locking
        private bool IsLocked(string path)
        {
            lock (_fileLock) {
                return _fileLock.Contains(path);
            }
        }

        private bool TryObtainExclusiveFileLock(string path)
        {
            if (!IsLocked(path)) {
                Log.Debug("Tried to process an ulocked path: {0}", path);
                return false;
            } 
            
            if (File.Exists(path)) {
                //If it's not a directory try to lock it exclusively to ensure nobody else is writing to it.
                try {
                    using (FileStream s = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)) {
                        s.Close();
                        return true;
                    }
                } catch {
                    Log.Debug("Could not acquire exclusive lock on file: {0}", path);
                    return false;
                }
            } else {
                return false;
            }
        }

        protected bool LockPath(string file)
        {
            lock (_fileLock) {
                if (_fileLock.Contains(file)) {
                    Log.Debug("Failed to lock path {0}", file);
                    return false;
                } else {
                    _fileLock.Add(file);
                    return true;
                }
            }
        }

        protected void UnlockPath(string file)
        {
            lock (_fileLock) {
                if (_fileLock.Contains(file)) {
                    _fileLock.Remove(file);
                }
            }
        }
        #endregion

        #region Postprocessing methods (move to another folder, etc)
        protected virtual void MarkAsProcessed(string path)
        {
            if (!string.IsNullOrEmpty(_archiveFolder)) {
                TryFiveTimes(new MoveFileOperation(path, Path.Combine(_archiveFolder, Path.GetFileName(path))));
                UnlockPath(path);
            }
        }

        protected virtual void Delete(string path)
        {
            TryFiveTimes(new DeleteFileOperation(path));
            UnlockPath(path);
        }

        protected virtual void MarkAsDeleted(string path)
        {
            if (_info.DeleteMeansDelete) {
                Delete(path);
            } else if (!string.IsNullOrEmpty(_deletedFolder)) {
                TryFiveTimes(new MoveFileOperation(path, Path.Combine(_deletedFolder, Path.GetFileName(path))));
            }
            UnlockPath(path);
        }

        protected virtual void MarkAsError(string path)
        {
            if (!string.IsNullOrEmpty(_errorFolder)) {
                TryFiveTimes(new MoveFileOperation(path, Path.Combine(_errorFolder, Path.GetFileName(path))));
                UnlockPath(path);
            }
        }

        /// <summary>
        /// Moves the file out of the queue and unlocks it from the Queue lock hash. The file will retain its name in the new location.
        /// </summary>
        /// <param name="targetFolder">Folder to move the file to</param>
        /// <param name="path">Path to the file to move</param>
        protected virtual void MoveFromQueue(string targetFolder, string path)
        {
            if (!string.IsNullOrEmpty(targetFolder)) {
                TryFiveTimes(new MoveFileOperation(path, Path.Combine(targetFolder, Path.GetFileName(path))));
                UnlockPath(path);
            }
        }
        #endregion

        #region Helpers
        private bool IsSubFolderOf(string _inputFolder, string _subFolder)
        {
            return !string.IsNullOrEmpty(_subFolder) && !string.IsNullOrEmpty(_inputFolder) && Path.GetDirectoryName(_subFolder).StartsWith(Path.GetDirectoryName(_inputFolder));
        }

        private string SetupConfiguredFolder(string basePath, string folderPath, string defaultFolderName)
        {
            string f = MakeRootedPath(basePath, folderPath);
            if (f == null) {
                f = MakeRootedPath(basePath, defaultFolderName);
            }
            EnsureFolderExists(f);

            return f;
        }

        protected virtual string EnsureFolderExists(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        private static string MakeRootedPath(string basePath, string folderPath)
        {
            string f = null;
            if (!string.IsNullOrWhiteSpace(folderPath)) {
                if (Path.IsPathRooted(folderPath)) {
                    f = folderPath;
                } else {
                    f = Path.GetFullPath(Path.Combine(basePath, folderPath));
                }
            }
            return f;
        }
        #endregion

        public void Dispose()
        {
            Log.Debug("Stopping {0}", _info.WatcherType);
            if (_periodicScanTimer != null) _periodicScanTimer.Dispose();
#if TRACE_QUEUE_SIZE
            if (_batchMonitorTimer != null) _batchMonitorTimer.Dispose();
#endif
            if (_watcher != null) _watcher.Dispose();
            _queue.Dispose();
        }
    }
}