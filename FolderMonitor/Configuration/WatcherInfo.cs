using System;
using System.Xml.Serialization;

namespace FolderMonitor
{
    [XmlRoot("watcher")]
    public class WatcherInfo
    {
        public WatcherInfo() { }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string WatcherType { get; set; }

        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("namespace")]
        public string Namespace { get; set; }

        [XmlAttribute("filter")]
        public string Filter { get; set; }

        [XmlAttribute("cleanupInterval")]
        public int CleanupInterval { get; set; }

        [XmlAttribute("inputFolder")]
        public string InputFolder { get; set; }

        [XmlAttribute("outputFolder")]
        public string OutputFolder { get; set; }

        [XmlAttribute("archiveFolder")]
        public string ArchiveFolder { get; set; }

        [XmlAttribute("deletedFolder")]
        public string DeletedFolder { get; set; }

        [XmlAttribute("errorFolder")]
        public string ErrorFolder { get; set; }

        [XmlAttribute("threads")]
        public int Threads { get; set; }

        [XmlAttribute("watchSubfolders")]
        public bool WatchSubfolders { get; set; }

        [XmlAttribute("noDefaultFolders")]
        public bool NoDefaultFolders { get; set; }

        [XmlAttribute("connectionString")]
        public string ConnectionString { get; set; }

        [XmlAttribute("deleteMeansDelete")]
        public bool DeleteMeansDelete { get; set; }
    }
}
