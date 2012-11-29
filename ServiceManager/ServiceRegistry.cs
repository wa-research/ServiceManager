using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ServiceManager
{
    class ServiceRegistry
    {
        #region Worker registry
        private Dictionary<string, ServiceInfo> _services = new Dictionary<string, ServiceInfo>();

        public ServiceInfo[] GetWorkers()
        {
            return _services.Values.ToArray();
        }

        public ServiceInfo GetWorker(string id)
        {
            if (_services.ContainsKey(id))
                return _services[id];
            else
                return null;
        }
        #endregion
        public ServiceRegistry()
        {
        }

        public void DiscoverAndStartServices(string basePath, string mask)
        {
            if (basePath == null || !Directory.Exists(basePath))
                throw new ArgumentException(string.Format("Invalid base path '{0}'", basePath ?? "<null>"));

            foreach (string d in Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)) {
                LoadServicesInDirectory(d, mask);
            }
        }

        private void LoadServicesInDirectory(string d, string dllMask)
        {
            foreach (string filePath in Directory.GetFiles(d, dllMask)) {
                string fp = Path.GetFullPath(filePath);
                LoadAssembly(fp);
            }
        }

        private void LoadAssembly(string filePath)
        {
            string wkid = HashFolderPath(filePath);

            if (CheckIfLoadedAndUpToDate(this._services, wkid))
                return;

            #region Create remote domain
            AssemblyName asmName = null;
            try {
                asmName = AssemblyName.GetAssemblyName(filePath);
            } catch {
                return;
            }

            AppDomain remoteDomain = null;
            try {
                remoteDomain = CreateRemoteDomain(filePath, asmName);
            } catch (Exception ex) {
                Console.WriteLine("ERROR: Could not create an AppDomain for '{0}'. ({1})", asmName.FullName, ex.Message);
                return;
            }
            #endregion

            //Inject proxy into appdomain
            ServiceProxy proxy = null;
            try {
                //Inject our local copy into assembly so that we don't have to deploy duplicates of shared dependencies to every worker folder
                //remoteDomain.Load(Assembly.GetAssembly(typeof(AsyncPipes.NamedPipeStreamClient)).GetName());

                proxy = (ServiceProxy)remoteDomain.CreateInstanceFromAndUnwrap(Assembly.GetAssembly(typeof(ServiceProxy)).Location, typeof(ServiceProxy).FullName);
                Console.WriteLine("\tProxy loaded into the remote domain");
            } catch (Exception ex) {
                Console.WriteLine("\tFailed to load Proxy: {0}", ex.Message);
                UnloadDomain(filePath, remoteDomain);
                return;
            }

            //Load worker into the domain
            try {
                if (!proxy.LoadWorker(asmName.FullName)) {
                    Console.WriteLine("Could not load any services from {0} (Most likely the assembly is missing an entry point)", asmName.FullName);
                    UnloadDomain(filePath, remoteDomain);
                    return;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                try {
                    AppDomain.Unload(remoteDomain);
                } catch (AppDomainUnloadedException ux) {
                    Console.WriteLine(ux.Message);
                }
                return;
            }
            _services.Add(wkid, new ServiceInfo { ID = wkid, Path = filePath, LastModified = File.GetLastWriteTime(filePath), Name = proxy.Name, Proxy = proxy });
        }

        public void StartServices()
        {
            foreach (var s in _services.Values) {
                s.Proxy.Start();
            }
        }
        public void StopServices()
        {
            foreach (var s in _services.Values) {
                s.Proxy.Stop();
            }
        }

        public void StartService(string fullPath)
        {
            var service = _services.Values.Where(s => s.Path == fullPath).FirstOrDefault();
            if (service != null) {
                service.Proxy.Start();
            }
        }

        public void StopService(string fullPath)
        {
            var service = _services.Values.Where(s => s.Path == fullPath).FirstOrDefault();
            if (service != null) {
                service.Proxy.Stop();
            }
        } 

        #region Create or unload a domain
        private AppDomain CreateRemoteDomain(string filePath, AssemblyName asmName)
        {
            string appBasePath = Path.GetDirectoryName(filePath);

            AppDomainSetup setup = new AppDomainSetup
            {
                ApplicationBase = appBasePath,
                ConfigurationFile = asmName.Name + ".dll.config",
                PrivateBinPath = appBasePath,
                ShadowCopyDirectories = appBasePath,
                ShadowCopyFiles = "true",
                ApplicationName = asmName.FullName,
                LoaderOptimization = LoaderOptimization.MultiDomainHost
            };
            return AppDomain.CreateDomain(asmName.FullName, null, setup);
        }

        private bool CheckIfLoadedAndUpToDate(Dictionary<string, ServiceInfo> dictionary, string wkid)
        {
            if (dictionary.ContainsKey(wkid)) {
                var prx = dictionary[wkid];

                string serviceName = dictionary[wkid].Name;
                DateTime curTime = File.GetLastWriteTime(wkid);

                // only add the service if it is not already running latest copy
                if (curTime.Ticks <= prx.LastModified.Ticks) {
                    //Log("Skipping '{0}' because it is already loaded.", prx.Name);
                    return true;
                } else {
                    // new copy, so stop current and clear out cache
                    // stop/unload service
                    //Log("Unloading old version of service '{0}:{1}'; will load updated binaries.", serviceName, wkid);
                    this.UnloadService(wkid);
                    dictionary.Remove(wkid);
                }
            }
            return false;
        }

        private void UnloadService(string wkid)
        {
            if (_services.ContainsKey(wkid)) {
                ServiceInfo worker = _services[wkid];

                try {
                    worker.Proxy.Stop();
                } catch { }

                try {
                    AppDomain.Unload(worker.AppDomain);
                } catch (Exception ex) {
                    //Log("Could not uload service {0}:{1} ({2})", worker.Name, wkid, ex.Message);
                }
            }
        }

        private void UnloadDomain(string filePath, AppDomain domain)
        {
            try {
                AppDomain.Unload(domain);
            } catch (AppDomainUnloadedException ex) {
                //Log("There was an error unloading domain at {0}: {1}", filePath, ex.Message);
            }
        }
        #endregion

        #region Create service ID
        public static string HashFolderPath(string text)
        {
            var SHA1 = new SHA1CryptoServiceProvider();

            byte[] arrayResult = SHA1.ComputeHash(Encoding.ASCII.GetBytes(text));
            StringBuilder sb = new StringBuilder(arrayResult.Length * 2);
            for (int i = 0; i < arrayResult.Length; i++) {
                sb.Append(arrayResult[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }
        #endregion
    }
}
