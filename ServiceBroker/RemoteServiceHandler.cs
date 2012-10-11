using System;
using System.Reflection;

namespace ServiceBroker
{
	/// <summary>
	/// Class used to run managed services in remote
	/// AppDomains.
	/// </summary>
	public class RemoteServiceHandler: MarshalByRefObject
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public RemoteServiceHandler() {	}

		/// <summary>
		/// Overrides the lifetime lease to return null, so the remote handles will never expire.  
		/// </summary>
		/// <returns>Null.</returns>
		/// <remarks>Since we are caching the handles for potentially very long periods, we don't want them to expire.</remarks>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// Service entry point type name.
		/// </summary>
		private string serviceEntryPointType;

		/// <summary>
		/// <see cref="IService"/> instance.
		/// </summary>
		private IService service;

        /// <summary>
        /// Service display name
        /// </summary>
        public string ServiceName { get; set; }

		/// <summary>
		/// Loads the assembly in the remote app domain and loads the IService data and instance.
		/// </summary>
		/// <param name="assemblyName">Full name of assembly to load.</param>
		/// <remarks>Specified assembly should apply <see cref="ServiceEntryPointAttribute"/> and implement <see cref="IService"/> for the type 
		/// specified by that attribute.</remarks>
		public bool LoadService(string assemblyName)
		{
            AppDomain cd = AppDomain.CurrentDomain;
            string basedir = cd.BaseDirectory;

			Assembly assembly = null;
			try
			{
				assembly = AppDomain.CurrentDomain.Load(assemblyName); 
			}
			catch (Exception ex)
			{
				throw new AssemblyLoadException(string.Format("Could not load assembly from path {0}.", basedir), assemblyName, ex);
			}
			// get the service point entry attribute
			ServiceEntryPointAttribute[] attribs = assembly.GetCustomAttributes(typeof(ServiceEntryPointAttribute), false) as ServiceEntryPointAttribute[];
			// false signals that no ServiceEntryPointAttribute was found
			if (attribs == null || attribs.Length == 0)
				return false;

			this.serviceEntryPointType = attribs[0].EntryPointTypeName;
			this.ServiceName = attribs[0].ServiceName;

			//Create IService instance
			try
			{
				this.service = (IService)assembly.CreateInstance(this.serviceEntryPointType, true);
			}
			catch (Exception ex)
			{
				throw new TypeInitializationException(this.serviceEntryPointType, ex);
			}
			return true;
		}

		/// <summary>
		/// Starts the service.
		/// </summary>
		public void StartService()
		{
			if (this.service != null)
				this.service.StartService();
            //This will write a log entry into the remote domain's log, not Generic Service's log
			Logger.Info(this.ServiceName + " started.");
		}

		/// <summary>
		/// Stops the service.
		/// </summary>
		public void StopService()
		{
			if (this.service != null)
				this.service.StopService();
            //This will write a log entry into the remote domain's log, not Generic Service's log
            Logger.Info(this.ServiceName + " stopped.");
		}
	}
}
