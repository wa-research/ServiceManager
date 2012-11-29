using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ServiceManager
{	
    [RunInstaller(true)]
    public class WindowsServiceProjectInstaller : Installer
    {
        public const string DEFAULT_NAME = "ECS Service Manager";

        public static void Uninstall(string[] args)
        {
            string name = args.Length == 2 ? args[1] : DEFAULT_NAME;
            try {

                TransactedInstaller ti = new TransactedInstaller();
                WindowsServiceProjectInstaller mi = WindowsServiceProjectInstaller.Create(name);
                ti.Installers.Add(mi);
                string path = string.Format("/assemblypath={0}", System.Reflection.Assembly.GetExecutingAssembly().Location);
                string[] cmdline = { path };
                InstallContext ctx = new InstallContext("", cmdline);
                ti.Context = ctx;
                ti.Uninstall(null);
            }
                //Swallow exception when we're trying to uninstall non-existent service
            catch { }
        }

        public static void Install(string[] args)
        {
            string name = args.Length == 2 ? args[1] : DEFAULT_NAME;
            try {
                TransactedInstaller ti = new TransactedInstaller();
                WindowsServiceProjectInstaller pi = WindowsServiceProjectInstaller.Create(name);
                ti.Installers.Add(pi);
                string path = string.Format("/assemblypath={0}",
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string[] cmdline = { path };
                InstallContext ctx = new InstallContext("", cmdline);
                ti.Context = ctx;
                ti.Install(new Hashtable());
            } catch (Exception ex) {
                Console.WriteLine("ERROR: {0}", ex.Message);
                Environment.Exit(1);
            }
        }

        private WindowsServiceProjectInstaller(string name)
        {
            ServiceProcessInstaller spi=new ServiceProcessInstaller();
            spi.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            spi.Password = null;
            spi.Username = null;
            ServiceInstaller si = new ServiceInstaller();
            si.ServiceName= name;
            this.Installers.Add(spi);
            this.Installers.Add(si);			 
        }

        public static WindowsServiceProjectInstaller Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DEFAULT_NAME;
            }
            return new WindowsServiceProjectInstaller(name);
        }
    }
}
