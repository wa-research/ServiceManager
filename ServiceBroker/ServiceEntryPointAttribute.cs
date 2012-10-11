using System;

namespace ServiceBroker
{
	/// <summary>
	/// Tells the service broker what type should be used
	/// as the service entry point.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class ServiceEntryPointAttribute : System.Attribute
	{
		private string serviceName;
		/// <summary>
		/// Gets or sets the service display name.
		/// </summary>
		public string ServiceName
		{
			get
			{
				return this.serviceName;
			}
			set
			{
				this.serviceName = value;
			}
		}
		
		private string entryPointTypeName;
		/// <summary>
		/// Gets or sets the service entry point type name.
		/// </summary>
		public string EntryPointTypeName
		{
			get
			{
				return this.entryPointTypeName;
			}
			set
			{
				this.entryPointTypeName = value;
			}
		}

		/// <summary>
		/// Sets the service entry point type name.
		/// </summary>
		/// <param name="serviceName">
		/// Service display name.</param>
		/// <param name="entryPointTypeName">
		/// Service entry point type name.</param>
		public ServiceEntryPointAttribute(
			string serviceName, string entryPointTypeName)
		{
			this.serviceName = serviceName;
			this.entryPointTypeName = entryPointTypeName;
		}
	}
}
