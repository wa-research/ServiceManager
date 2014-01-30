using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ServiceManager
{
    class ServiceRegistry
    {
        #region Public interface
        private Dictionary<string, ServiceInfo> _services = new Dictionary<string, ServiceInfo>();

        public ServiceInfo[] GetServices()
        {
            return _services.Values.ToArray();
        }

        public ServiceInfo GetServiceInfo(string id)
        {
            if (_services.ContainsKey(id))
                return _services[id];
            else
                return null;
        }

        public void DiscoverServices(string basePath, string mask)
        {
            if (basePath == null || !Directory.Exists(basePath))
                throw new ArgumentException(string.Format("Invalid base path '{0}'", basePath ?? "<null>"));

            foreach (string d in Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)) {
                LoadServicesInDirectory(d, mask);
            }
        }

        public void StartServices()
        {
            foreach (var s in _services.Values) {
                try {
                    Log("Starting {0} ({1})", s.Name, s.Path);
                    s.Proxy.Start();
                } catch (Exception ex) {
                    Log("Could not start service {0}: {1}", s.Name, ex.ToString());
                }
            }
        }

        public void StopServices()
        {
            foreach (var s in _services.Values.ToArray()) {
                StopService(s.Path);
            }
        }

        public void StartService(string fullPath)
        {
            LoadAssembly(fullPath);
            var service = _services.Values.Where(s => s.Path == fullPath).FirstOrDefault();
            if (service != null) {
                service.Proxy.Start();
            }
        }

        public void StopService(string fullPath)
        {
            var service = _services.Values.Where(s => s.Path == fullPath).FirstOrDefault();
            if (service != null) {
                UnloadService(service.ID);
            }
        } 
        #endregion

        private void LoadServicesInDirectory(string d, string dllMask)
        {
            foreach (string filePath in Directory.GetFiles(d, dllMask)) {
                string fp = Path.GetFullPath(filePath);
                LoadAssembly(fp);
            }
        }

        private void LoadAssembly(string filePath)
        {
            string wkid = CreateUniqueID(filePath);

            if (!File.Exists(filePath))
                return;

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
                Log("ERROR: Could not create an AppDomain for '{0}'. ({1})", asmName.FullName, ex.Message);
                return;
            }
            #endregion

            //Inject proxy into appdomain
            ServiceProxy proxy = null;
            try {
                //Inject our local copy into assembly so that we don't have to deploy duplicates of shared dependencies to every svc folder
                //remoteDomain.Load(Assembly.GetAssembly(typeof(AsyncPipes.NamedPipeStreamClient)).GetName());

                proxy = (ServiceProxy)remoteDomain.CreateInstanceFromAndUnwrap(Assembly.GetAssembly(typeof(ServiceProxy)).Location, typeof(ServiceProxy).FullName);
                Log("Proxy loaded into the remote domain {0}", remoteDomain.FriendlyName);
            } catch (Exception ex) {
                Log("Failed to load Proxy: {0}", ex.Message);
                UnloadDomain(filePath, remoteDomain);
                return;
            }

            //Load svc into the domain
            try {
                if (!proxy.LoadService(asmName.FullName)) {
                    Log("Could not load any services from {0} (Most likely the assembly is missing an entry point)", asmName.FullName);
                    UnloadDomain(filePath, remoteDomain);
                    return;
                }
            } catch (Exception ex) {
                Log("Error loading service {0}: {1}", asmName.FullName, ex.Message);
                try {
                    AppDomain.Unload(remoteDomain);
                } catch (AppDomainUnloadedException ux) {
                    Log(ux.Message);
                }
                return;
            }
            _services.Add(wkid, new ServiceInfo { ID = wkid, Path = filePath, LastModified = File.GetLastWriteTime(filePath), Name = proxy.Name, Proxy = proxy, AppDomain = remoteDomain });
        }

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

                string serviceName = prx.Name;
                DateTime curTime = File.GetLastWriteTime(prx.Path);

                // only add the service if it is not already running latest copy
                if (curTime.Ticks <= prx.LastModified.Ticks) {
                    Log("Skipping '{0}' because it is already loaded.", prx.Name);
                    return true;
                } else {
                    // new copy, so stop current and clear out cache
                    Log("Unloading old version of service '{0}:{1}'; will load updated binaries.", serviceName, wkid);
                    this.UnloadService(wkid);
                }
            }
            return false;
        }

        public static void Log(string format, params object[] args)
        {
            int thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int process = Process.GetCurrentProcess().Id;

            string meta = string.Format("{0} [{1}:{2}] ", DateTime.UtcNow.ToString("s"), process, thread);
            Console.WriteLine(meta + format, args);
        }

        private void UnloadService(string id)
        {
            if (_services.ContainsKey(id)) {
                ServiceInfo svc = _services[id];

                try {
                    svc.Proxy.Stop();
                } catch (Exception ex) {
                    Log("Error while stopping service {0} [{1}]: {2}", svc.Name, id, ex.Message);
                }

                try {
                    AppDomain.Unload(svc.AppDomain);
                } catch (Exception ex) {
                    Log("Could not unload service {0} [{1}]: {2}", svc.Name, id, ex.Message);
                } finally {
                    _services.Remove(id);
                }
            }
        }

        private void UnloadDomain(string filePath, AppDomain domain)
        {
            try {
                AppDomain.Unload(domain);
            } catch (AppDomainUnloadedException ex) {
                Log("There was an error unloading domain at {0}: {1}", filePath, ex.Message);
            }
        }

        #region Create service ID
        /// <summary>
        /// A normalized full path is a good unique ID
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CreateUniqueID(string path)
        {
            return path.ToLower();
        }
        #endregion
    }
}
