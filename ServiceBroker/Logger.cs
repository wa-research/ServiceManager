using System;
using System.Diagnostics;

namespace ServiceBroker
{
	/// <summary>
	/// Utility class for logging
	/// </summary>
	public sealed class Logger 
	{
		private Logger()	{}

        /// <summary>
        /// Log exception without providing additional messages.
        /// </summary>
        /// <param name="ex"></param>
        internal static void LogException(Exception ex)
        {
        }

		/// <summary>
		/// Logs an exception using the MS exception
		/// management app blocks.
		/// </summary>
        /// <param name="message"></param>
		/// <param name="ex"></param>
		public static void LogException(string message, Exception ex)
		{
		}

        /// <summary>
        /// Write informational message to log
        /// </summary>
        /// <param name="message"></param>
        public static void Info(object message)
        {
        }

        /// <summary>
        /// Write informational message to log
        /// </summary>
        /// <param name="args"></param>
        /// <param name="format"></param>
        public static void Info(string format, params object[] args)
        {
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="message"></param>
        public static void Error(object message)
        {
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="args"></param>
        /// <param name="format"></param>
        public static void Error(string format, params object[] args)
        {
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(object message)
        {
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="args"></param>
        /// <param name="format"></param>
        public static void Debug(string format, params object[] args)
        {
        }

	}
}