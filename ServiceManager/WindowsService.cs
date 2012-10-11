using System;
using System.Collections;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Configuration;

namespace ServiceManager {
    /// <summary>
    /// .NET Service Manager Windows Service Class
    /// </summary>
    /// <remarks>Original text on http://www.15seconds.com/issue/040624.htm</remarks>
    public class WindowsService : System.ServiceProcess.ServiceBase	
    {
        public const string DEFAULT_SERVICE_NAME = "Simple Service Manager";
        protected static log4net.ILog _log = log4net.LogManager.GetLogger(typeof(WindowsService));
        
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        /// <summary>
        /// Watches the application directory for 
        /// changes, creates, and deletes.
        /// </summary>
        private FileSystemWatcher serviceWatcher = null;
        /// <summary>
        /// Manages loading, starting, stopping, and unloading
        /// of services that target the <see cref="ServiceBroker"/>
        /// interface.
        /// </summary>
        private ServiceBroker.ServiceBroker broker = null;
        /// <summary>
        /// Keeps a record of the last change time to a directory,
        /// so change events won't fire too many times.
        /// </summary>
        DateTime lastChangeTime = DateTime.Now;
        /// <summary>
        /// Folder to scan for services
        /// </summary>
        private string _servicesBaseFolder;
        /// <summary>
        /// Keeps a record of the last file path changed.
        /// </summary>
        private string lastFilePath = string.Empty;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WindowsService() : this(null) { }

        public WindowsService(string basePath) 	{
            // This call is required by the Windows.Forms Component Designer.
            InitializeComponent();
            _log.Debug("Service started");

            if (string.IsNullOrWhiteSpace(basePath)) {
                basePath = ConfigurationManager.AppSettings["ServicesBaseFolder"];
            }
            if (string.IsNullOrWhiteSpace(basePath)) {
                basePath = AppDomain.CurrentDomain.BaseDirectory;
            }
            _servicesBaseFolder = Path.GetFullPath(basePath);
            _log.Debug(string.Format("Watching path: {0} and its subfolders for service assemblies named *Service.dll", _servicesBaseFolder));
            if (!Directory.Exists(_servicesBaseFolder)) {
                _log.ErrorFormat("Service base directory {0} does not exist or permissions are not correct. Exiting.", _servicesBaseFolder);
                Stop();
                return;
            }
            // initialize the service broker
            this.broker = new ServiceBroker.ServiceBroker();
            // initialize watcher to watch the process directory
            this.serviceWatcher = new FileSystemWatcher();
            this.serviceWatcher.Path = _servicesBaseFolder;
            this.serviceWatcher.IncludeSubdirectories = true;
            // only monitor changes to .DLL files
            this.serviceWatcher.Filter = "*Service.dll";
            // only handle changed, created, and deleted events
            //NOTE: Make sure all dependency DLLs are loaded first. If the changed event
            //for the service is processed before all other DLLs are loaded, StartService() might fail.
            this.serviceWatcher.Changed += new FileSystemEventHandler(File_OnChanged);
            this.serviceWatcher.Created += new FileSystemEventHandler(File_OnChanged);
            this.serviceWatcher.Deleted += new FileSystemEventHandler(File_OnChanged);
            // tell it to start watching
            this.serviceWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Runs the service.
        /// </summary>
        private static void Run()
        {
            // instantiate and run service
            ServiceBase.Run(new ServiceBase[] { new WindowsService() });
        }


        /// <summary>
        /// Process entry point.
        /// </summary>
        static void Main(string[] args) 
        {
            string opt = null;
            // check for arguments
            if (args.Length > 0)
            { 
                opt = args[0];
                if(opt!=null && opt.ToLower()=="/install")
                {
                    try
                    {
                        string name = args.Length == 2 ? args[1] : DEFAULT_SERVICE_NAME;

                        TransactedInstaller ti = new TransactedInstaller();
                        WindowsServiceProjectInstaller pi = WindowsServiceProjectInstaller.Create(name);
                        ti.Installers.Add(pi);
                        String path = String.Format("/assemblypath={0}",
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                        String[] cmdline = { path };
                        InstallContext ctx = new InstallContext("", cmdline);
                        ti.Context = ctx;
                        ti.Install(new Hashtable());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: {0}", ex.Message);
                        Environment.Exit(1);
                    }
                }
                else if (opt !=null && opt.ToLower()=="/uninstall")
                {
                    try
                    {
                        string name = args.Length == 2 ? args[1] : DEFAULT_SERVICE_NAME;

                        TransactedInstaller ti = new TransactedInstaller();
                        WindowsServiceProjectInstaller mi = WindowsServiceProjectInstaller.Create(name);
                        ti.Installers.Add(mi);
                        String path = String.Format("/assemblypath={0}",
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                        String[] cmdline = { path };
                        InstallContext ctx = new InstallContext("", cmdline);
                        ti.Context = ctx;
                        ti.Uninstall(null);
                    }
                    //Swallow exception when we're trying to uninstall non-existent service
                    catch { }
                }
                else if (opt != null && ("/console".Equals(opt, StringComparison.OrdinalIgnoreCase) || "--console".Equals(opt, StringComparison.OrdinalIgnoreCase)))
                {
                    string watchedFolder = null;
                    if (args.Length > 1) {
                        watchedFolder = args[1];
                    }
                    if (!Directory.Exists(watchedFolder)) {
                        Console.WriteLine("Service base folder {0} does not exist.", watchedFolder);
                        return;
                    }
                    WindowsService srv = new WindowsService(watchedFolder);
                    srv.OnStart(args);

                    Console.WriteLine("Watching folders rooted at '{0}'", srv._servicesBaseFolder);
                    Console.WriteLine();
                    Console.WriteLine("Type 'exit' to end.");
                    string input = Console.In.ReadLine();
                    while (input == null || !input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        input = Console.In.ReadLine();
                    }

                    srv.OnStop();
                }

                if(opt==null)
                {
                    Run();
                }
            }
            else 
                Run();
        }

        /// <summary>
        /// Required by the designer to init designable settings
        /// </summary>
        private void InitializeComponent() 
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = DEFAULT_SERVICE_NAME;
        }

        /// <summary>
        /// FileSystemWatcher Event Handler
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="e">Args</param>
        public void File_OnChanged(object source, FileSystemEventArgs e) 
        {
            // Prevent multiple calls from being made on a single change
            TimeSpan span = DateTime.Now.Subtract(lastChangeTime);
            // if it's been more than two seconds since the last change
            // or the last change is a different file, try to start it up
            if (span.TotalSeconds > 2 || lastFilePath != e.FullPath)
            {
                // wait a second for any locks to be released
                // before taking actions
                System.Threading.Thread.Sleep(1000);
                // check the change type
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Created:
                        // try to start a service in the
                        // changed DLL
                        _log.DebugFormat("On_Changed event triggered for assembly {0}", e.FullPath);
                        broker.StartService(e.FullPath);
                        lastChangeTime = DateTime.Now;
                        lastFilePath = e.FullPath;
                        break;
                    case WatcherChangeTypes.Deleted:
                        _log.DebugFormat("'Deleted' event triggered for assembly {0}", e.FullPath);
                        // try to stop a service in the
                        // deleted DLL
                        broker.StopService(e.FullPath);
                        break;
                }
            }
        }

        /// <summary>
        /// Required by the designer
        /// </summary>
        /// <param name="disposing">Force disposing.</param>
        protected override void Dispose(bool disposing) 
        {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing );
        }

        /// <summary>
        /// Runs on service start.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        protected override void OnStart(string[] args) 
        {
            if (this.broker != null) 
                this.broker.StartServices(_servicesBaseFolder);
        }

        /// <summary>
        /// Runs after pause.
        /// </summary>
        protected override void OnContinue() 
        {
            if (this.broker != null)
                this.broker.StartServices(_servicesBaseFolder);
        }
 
        /// <summary>
        /// Runs on service stop.
        /// </summary>
        protected override void OnStop() 
        {
            if (this.broker != null)
                this.broker.StopServices();
        }

        /// <summary>
        /// Runs on service pause.
        /// </summary>
        protected override void OnPause() 
        {
            this.broker.StopServices();
        }
    }
}
