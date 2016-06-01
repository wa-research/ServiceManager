# Folder Monitor 

The folder monitor allows one to create ServiceManager serivices that watch folders and react to file notifications in those folders.

## Implementation

The queue watcher has to implement the HandleFile method and process each file it is handed to:

    public class QueueWatcher : FileWatcherBase
    {
        public QueueWatcher(WatcherInfo info) : base(info) { }

        protected override void HandleFile(string path, string name)
        {
            bool delete = true;
            try {
                // Do work here with the file
            } catch (Exception ex) {
                Log.Error("Could not process file '{0}' ({1})", path, ex.Message);
                MarkAsError(path);
                delete = false;
            }
            if (delete)
                MarkAsDeleted(path);
            else
                MarkAsProcessed(path);
        }
    }


The override has to call one of Mark* methods:

MarkAsProcessed - the file will be moved to 'Processed' subfolder
MarkAsError - the file will be moved to 'Error' subfolder
MarkAsDeleted - the file will be immediately deleted, or be moved to 'Deleted' subfolder if deleteMeansDelete is set to false.

## Configuration

The service configuration file must contain a section with folder monitor definitions.

For example, one can watch a temp folder for text files, and then read each file and insert its contents in the database:

    <?xml version="1.0" encoding="utf-8" ?>
    <configuration>
        <configSections>
            <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
            <section name="watchers" type="FolderMonitor.FolderMonitorConfigHandler, FolderMonitor" />
        </configSections>
        <watchers>
            <watcher name="Mail Event Importer" type="EventImporter.EventQueueWatcher, EventImporterService" inputFolder="C:\temp" deleteMeansDelete="true" filter="*.txt" cleanupInterval="60" >
                <settings>
                </settings>
            </watcher>
        </watchers>
    </configuration


### Watcher Configuration Attributes

name - watcher name. Should be unique in a list of watchers.

type - full type name to instantiate the watcher.

url - optional url to pass to the watcher (Use for watchers that upload files somewhere).

filter - only look for files of specified type. Defaults to *.*.

cleanupInterval - interval in seconds to run the scavenger thread to pick up any files that were not processed with normal file notifications.

inputFolder - folder that the watcher is watching for file notifications. Any new file in this folder will be processed by the watcher service in the HandleFile method.

outputFolder - store the processed files in this folder.

archiveFolder - if the watcher calls 'Archive' method, the files will end up in this folder.

deletedFolder - store all files that the watcher decided to delete.

errorFolder - the folder to store all files that the watcher failed to process.

threads - number of threads to use for the internal producer-consumer queue. Defaults to 4. This is an advanced setting and does not need changing.

watchSubfolders - optionally watch subfolders of the input folder. ouputFolder must be in a different location than the default setup, which creates output folders under the input folder.

noDefaultFolders - do not create default folders.

connectionString - optional database connection string to pass to the watcher.

deleteMeansDelete - processed files will really be deleted. If false (default), then processed files will not be deleted, to allow one do debug the service without the need to continuously generate new files.