using System;

namespace ServiceBroker
{
	/// <summary>
	/// Interface that all services using the
	/// ServiceBroker must implement on the type specified
	/// by the <see cref="ServiceEntryPointAttribute.EntryPointTypeName"/>.
	/// </summary>
	public interface IService 
	{
		/// <summary>
		/// Starts the service functionality.
		/// </summary>
		void StartService();
		/// <summary>
		/// Stops the service functionality.
		/// </summary>
		void StopService();
	}
}
