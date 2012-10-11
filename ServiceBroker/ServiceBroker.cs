using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace ServiceBroker
{
	/// <summary>
	/// Provides the engine for loading, starting, stopping, 
	/// and unloading services that apply the 
	/// <see cref="ServiceEntryPointAttribute"/> and implement
	/// the <see cref="IService"/> interface.
	/// </summary>
	public sealed class ServiceBroker
	{
		/// <summary>
		/// Caches file to service name mapping.
		/// </summary>
		/// <remarks>Key: Original DLL file path; 
		/// Value: Service Name as specified by 
		/// <see cref="ServiceEntryPointAttribute.ServiceName"/>.
		/// </remarks>
		private HybridDictionary serviceNames = new HybridDictionary(10);

		/// <summary>
		/// Caches service application domains.
		/// </summary>
		/// <remarks>Key: Service Name as specified by 
		/// <see cref="ServiceEntryPointAttribute.ServiceName"/>; 
		/// Value: Reference to related 
		/// <see cref="System.AppDomain"/> instance.</remarks>
		private HybridDictionary serviceAppDomains = new HybridDictionary(10);

		/// <summary>
		/// Caches service handlers.
		/// </summary>
		/// <remarks>Key: Service Name as specified by 
		/// <see cref="ServiceEntryPointAttribute.ServiceName"/>; 
		/// Value: Reference to related 
		/// <see cref="RemoteServiceHandler"/> instance.</remarks>
		private HybridDictionary services = new HybridDictionary(10);

		
		/// <summary>
		/// Caches service file last modified times.
		/// </summary>
		/// <remarks>Key: Service Name as specified by 
		/// <see cref="ServiceEntryPointAttribute.ServiceName"/>; 
		/// Value: The <see cref="DateTime"/> that the
		/// service file was last modified.</remarks>
		private HybridDictionary serviceLastModified = new HybridDictionary(10);

		/// <summary>
		/// Attempts to start a service in the file
		/// specified by <i>filePath</i>.
		/// </summary>
		/// <param name="filePath">Should be the path to a .NET assembly.</param>
		/// <remarks>This method will ignore 
		/// any Microsoft.ApplicationBlocks assemblies as
		/// well as the ServiceBroker assembly.
		/// <p>It first attempts to load the assembly name.
		/// If the file is not a .NET assembly, it will skip it 
		/// at this point.</p></remarks>
		public void StartService(string filePath)
		{
#if DEBUG
            Logger.Debug("[ServiceBroker.StartService] {0} - starting services it might contain", filePath);
#endif
			string serviceName;
            string fileName = Path.GetFileName(filePath);
			// skipping this assembly and the app blocks assemblies
			if (fileName.IndexOf("Microsoft.ApplicationBlocks") != -1 ||
				fileName == "ServiceBroker.dll")
				return;
			try
			{
				// Get the assembly name -- this will not load the assembly
				// this is also a .NET checker--this will fail if the DLL is not 
				// a .NET assembly
				AssemblyName asmName = null;
				try
				{
					asmName = AssemblyName.GetAssemblyName(filePath);
				}
				catch 
				{
                    Logger.Debug("Could not get assembly name from '{0}'; skipping.", filePath);
					return;
				}

				// check if assembly service already loaded
				if (this.serviceNames.Contains(filePath))
				{
					serviceName = this.serviceNames[filePath].ToString();
					DateTime curTime = File.GetLastWriteTime(filePath);
					// only add the service if it is not already running latest copy
					if (curTime.Ticks <= ((DateTime)this.serviceLastModified[serviceName]).Ticks)
					{
                        Logger.Debug("Skipping '{0}' because it is already loaded.", serviceName);
						return;
					}
					else // new copy, so stop current and clear out cache
					{
						// stop/unload service
						this.UnloadService(serviceName);
						// remove from file path cache
						this.serviceNames.Remove(filePath);
						Logger.Debug("Service '{0}' removed.", serviceName);
					}
				}

				// create the service app domain
				AppDomain svcDomain = null;
				try
				{
					AppDomainSetup setup = new AppDomainSetup();
					// use the process base directory as the new domain base and bin path
					setup.ApplicationBase = Path.GetDirectoryName(filePath);
                    setup.ConfigurationFile = asmName.Name + ".dll.config";
					setup.PrivateBinPath = setup.ApplicationBase;
					// use the assembly full name as friendly name
					setup.ApplicationName = asmName.FullName;
					// the base will be the shadow copy 'from' directory
					setup.ShadowCopyDirectories = setup.ApplicationBase;
					setup.ShadowCopyFiles = "true";
					// create the domain with no CAS evidence
					svcDomain = AppDomain.CreateDomain(asmName.FullName, null, setup);
				}
				catch (Exception ex)
				{
					Logger.Error("Could not create an AppDomain for '{0}'; skipping. ({1})", asmName.FullName, ex.Message);
					return;
				}

				// Get remote service handler
				RemoteServiceHandler svc = null;
				try
				{
					// This call will actually create an instance
					// of the service handler in the service domain
					// and return a remoting proxy for us to act on.
					// This is important because we don't want to 
					// load any type info from the service assembly
					// into this assembly
					svc = (RemoteServiceHandler)  
						svcDomain.CreateInstanceFromAndUnwrap(
						svcDomain.BaseDirectory + "\\ServiceBroker.dll", 
						"ServiceBroker.RemoteServiceHandler");
				}
				catch
				{
					// unload domain
					AppDomain.Unload(svcDomain);
					Logger.Debug(svcDomain.BaseDirectory + asmName.FullName + ": could not load ServiceBroker remote service handler, skipping.");
					return;
				}

				// try loading the service
				try
				{
					// this will attempt to load the service assembly 
					// in the service domain and then attempt to reflect
					// on the assembly to get the service entry point type 
					// and instantiate it if found
					if (!svc.LoadService(asmName.FullName))
					{
                        Logger.Error("Could not load any services from {0} (Most likely the assembly is missing ServiceEntryPoint attribute?)", asmName.FullName);               
						AppDomain.Unload(svcDomain);
						return;
					}
				}
				catch (Exception ex)
				{
					// unload domain
					AppDomain.Unload(svcDomain);
					Logger.LogException(ex);
					return;
				}

                Logger.Debug("Starting service {0} from assembly {1}", svc.ServiceName, asmName.FullName);
				svc.StartService();
				// get service name locally to minimize remoting traffic
				serviceName = svc.ServiceName;
				// cache service instance, current last modified, 
				// service name, and the appdomain reference
				this.serviceNames.Add(filePath, serviceName);
				this.services.Add(serviceName, svc);
				this.serviceLastModified.Add(serviceName, File.GetLastWriteTime(filePath));
				this.serviceAppDomains.Add(serviceName, svcDomain);
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
			}
		}

		/// <summary>
		/// Starts new service assemblies.
		/// </summary>
        /// <param name="basePath">Base of the folder tree to search for folders</param>
		/// <remarks>Attempts to load and start services for all DLLs in the 
		/// app domain base directory.</remarks>
		public void StartServices(string basePath) 
		{
            foreach (string d in Directory.GetDirectories(basePath, "*.*", SearchOption.AllDirectories)) {
                StartServicesInFolder(d);
            }
        }

        /// <summary>
        /// Scan all .dll files in the specified <paramref name="path"/> and start any services found
        /// </summary>
        /// <param name="path">Folder to search for services</param>
        public void StartServicesInFolder(string path)
        {
            foreach (string filePath in Directory.GetFiles(path, "*Service.dll")) {
                this.StartService(filePath);
            }
		}

		/// <summary>
		/// Stops and unloads all loaded services.
		/// </summary>
		public void StopServices()
		{
			// unload all services
			foreach (string serviceName in this.serviceNames.Values)
				this.UnloadService(serviceName);
			// ensure clearing of service caches
			this.services.Clear();
			this.serviceAppDomains.Clear();
			this.serviceLastModified.Clear();
			this.serviceNames.Clear();
		}

		/// <summary>
		/// Stop and unload service loaded from specified file.
		/// </summary>
		/// <param name="filePath">Path to service assembly.</param>
		public void StopService(string filePath)
		{
			if (this.serviceNames.Contains(filePath))
			{
				this.UnloadService(Convert.ToString(this.serviceNames[filePath]));
				this.serviceNames.Remove(filePath);
			}
		}

		/// <summary>
		/// Stop and unload service.
		/// </summary>
		/// <param name="serviceName">Display name of service.</param>
		private void UnloadService(string serviceName)
		{
			// stop service
			RemoteServiceHandler service = 
				this.services[serviceName] as RemoteServiceHandler;
			if (service != null)
			{
				try
				{
					service.StopService();
				}
				catch (Exception ex)
				{
					Logger.LogException(ex);
				}
			}
			// remove service handler reference
			this.services.Remove(serviceName);
			// unload service app domain
			AppDomain svcDomain = 
				this.serviceAppDomains[serviceName] as AppDomain;
			if (svcDomain != null)
			{
				try
				{
					AppDomain.Unload(svcDomain);
				}
				catch (Exception ex)
				{
					Logger.LogException(ex);
				}
			}
			// remove service domain reference
			this.serviceAppDomains.Remove(serviceName);
			// remove last mod flag
			this.serviceLastModified.Remove(serviceName);
		}

	}
}
