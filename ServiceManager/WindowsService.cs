using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;

namespace ServiceManager {
    /// <summary>
    /// .NET Service Manager Windows Service Class
    /// </summary>
    /// <remarks>
    /// Inspired by "The Perfect Service" by Ambrose J. Little
    /// http://aspalliance.com/749_The_Perfect_Service__Part_1
    /// </remarks>
    public class WindowsService : System.ServiceProcess.ServiceBase	
    {
        private FileSystemWatcher serviceWatcher = null;
        private ServiceRegistry broker = null;
        DateTime lastChangeTime = DateTime.Now;
        private string lastFilePath = string.Empty;
        private string _servicesBaseFolder;

        public WindowsService() : this(null) { }

        public WindowsService(string basePath) 	{

            this.ServiceName = WindowsServiceProjectInstaller.DEFAULT_NAME;

            if (string.IsNullOrWhiteSpace(basePath)) {
                basePath = ConfigurationManager.AppSettings["ServicesBaseFolder"];
            }
            if (string.IsNullOrWhiteSpace(basePath)) {
                basePath = AppDomain.CurrentDomain.BaseDirectory;
            }
            _servicesBaseFolder = Path.GetFullPath(basePath);
            if (!Directory.Exists(_servicesBaseFolder)) {
                Log("Service base directory {0} does not exist or permissions are not correct. Exiting.", _servicesBaseFolder);
                Stop();
                return;
            }
            this.broker = new ServiceRegistry();
            this.serviceWatcher = new FileSystemWatcher();
            this.serviceWatcher.Path = _servicesBaseFolder;
            this.serviceWatcher.IncludeSubdirectories = true;
            this.serviceWatcher.Filter = "*Service.dll";
            this.serviceWatcher.Changed += new FileSystemEventHandler(File_OnChanged);
            this.serviceWatcher.Created += new FileSystemEventHandler(File_OnChanged);
            this.serviceWatcher.Deleted += new FileSystemEventHandler(File_OnChanged);
            this.serviceWatcher.EnableRaisingEvents = true;
        }

        private static void Run()
        {
            ServiceBase.Run(new ServiceBase[] { new WindowsService() });
        }

        /// <summary>
        /// Process entry point.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive) {
                string opt = args.Length > 0 ? args[0] : null;
                if (opt != null) {
                    if (opt.ToLower() == "/install") {
                        WindowsServiceProjectInstaller.Install(args);
                        Environment.Exit(0);
                    } else if (opt.ToLower() == "/uninstall") {
                        WindowsServiceProjectInstaller.Uninstall(args);
                        Environment.Exit(0);
                    }
                }
                RunConsole(args, opt);
            } else {
                Run();
            }
        }

        private static void RunConsole(string[] args, string watchedFolder)
        {
            WindowsService srv = new WindowsService(watchedFolder);

            srv.Log("Watching folders rooted at '{0}'", srv._servicesBaseFolder);

            srv.OnStart(args);

            Console.WriteLine("___________________________________________________");
            Console.WriteLine("Type 'exit' to end, 'list' to list loaded services.");

            string input = null;
            while (input == null || !input.Equals("exit", StringComparison.OrdinalIgnoreCase)) {
                input = Console.In.ReadLine();
                if (input.Equals("list", StringComparison.OrdinalIgnoreCase))
                    ListLoadedServices(srv.broker.GetServices());
            }

            srv.OnStop();

            Environment.Exit(0);
        } 

        private static void ListLoadedServices(ServiceInfo[] serviceInfo)
        {
            foreach (var si in serviceInfo)
                Console.WriteLine(si.Path);
        }

        private void File_OnChanged(object source, FileSystemEventArgs e) 
        {
            // Prevent multiple calls from being made on a single change
            TimeSpan span = DateTime.Now.Subtract(lastChangeTime);
            if (span.TotalSeconds > 2 || lastFilePath != e.FullPath)
            {
                // wait a second for any locks to be released before taking actions
                System.Threading.Thread.Sleep(1000);
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Created:
                        Log("'Created' event triggered for assembly {0}", e.FullPath);
                        try {
                            broker.StartService(e.FullPath);
                        } catch (Exception ex) {
                            Log("Could not start service {0}: {1}", e.FullPath, ex.ToString());
                        }
                        lastChangeTime = DateTime.Now;
                        lastFilePath = e.FullPath;
                        break;
                    case WatcherChangeTypes.Deleted:
                        Log("'Deleted' event triggered for assembly {0}", e.FullPath);
                        try {
                            broker.StopService(e.FullPath);
                        } catch (Exception ex) {
                            Log("Error while stopping service {0}: {1}", e.FullPath, ex.ToString());
                        }
                        break;
                }
            }
        }

        protected override void OnStart(string[] args) 
        {
            if (this.broker != null) {
                this.broker.DiscoverServices(_servicesBaseFolder, "*Service.dll");
                broker.StartServices();
            }
        }

        protected override void OnContinue() 
        {
            if (this.broker != null)
                this.broker.DiscoverServices(_servicesBaseFolder, "*Service.dll");
        }
 
        protected override void OnStop() 
        {
            if (this.broker != null)
                this.broker.StopServices();
        }

        protected override void OnPause() 
        {
            this.broker.StopServices();
        }

        private void Log(string fmt, params object[] arg0)
        {
            ServiceRegistry.Log(fmt, arg0);
        }
    }
}
