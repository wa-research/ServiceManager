using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ServiceManager
{	
    [RunInstaller(true)]
    public class WindowsServiceProjectInstaller : Installer
    {
        public const string DEFAULT_NAME = "ECS Service Manager";

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
