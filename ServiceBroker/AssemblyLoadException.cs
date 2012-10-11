using System;

namespace ServiceBroker
{
	/// <summary>
	/// Thrown when there is an error loading an assembly.
	/// </summary>
    [Serializable]
	public class AssemblyLoadException : System.Exception
	{
		private string assemblyName;
		/// <summary>
		/// Full name of assembly being loaded
		/// when exception occurred.
		/// </summary>
		public string AssemblyName
		{
			get			{				return this.assemblyName;			}
			set			{				this.assemblyName = value;			}
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public AssemblyLoadException() : base()
		{
		}
   
		/// <summary>
		/// Constructor accepting a string message.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="assemblyName"><see cref="AssemblyName"/></param>
		public AssemblyLoadException(string message, string assemblyName) : base(message)
		{
			this.assemblyName = assemblyName;
		}
   
		/// <summary>
		/// Constructor accepting a string message and an 
		/// inner exception which will be wrapped by this 
		/// custom exception class.
		/// </summary>
		/// <param name="message">Exception message.</param>
		/// <param name="assemblyName"><see cref="AssemblyName"/></param>
		/// <param name="inner">Inner exception instance.</param>
		public AssemblyLoadException(string message, string assemblyName, Exception inner) : base(message, inner)
		{
			this.assemblyName = assemblyName;
		}

        /// <summary>
        /// Constructor needed for serialization when exception propagates from a remoting server to the client.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected AssemblyLoadException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

		/// <summary>
		/// Gets a string representation of this object.
		/// </summary>
		/// <returns>String representation of this object.</returns>
		public override string ToString()
		{
			return "Assembly Name: " + this.assemblyName + "; " + base.ToString();
		}

	}
}
