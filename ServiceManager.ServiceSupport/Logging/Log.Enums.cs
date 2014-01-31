#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;

namespace ServiceManager.ServiceSupport.Logging
{

	/// <summary>
	/// Defines the various levels of logging
	/// </summary>
	public enum LogLevels : int
	{
		/// <summary> Logging is disabled </summary>
		None = System.Diagnostics.SourceLevels.Off,
		/// <summary> Logs a Fatal error </summary>
		Critical = System.Diagnostics.SourceLevels.Critical,
		/// <summary> Logs an Error </summary>
		Error = System.Diagnostics.SourceLevels.Error,
		/// <summary> Logs a Warning </summary>
		Warning = System.Diagnostics.SourceLevels.Warning,
		/// <summary> Logs an Informational message </summary>
		Info = System.Diagnostics.SourceLevels.Information,
		/// <summary> Logs a Verbose message </summary>
		Verbose = System.Diagnostics.SourceLevels.Verbose,
		// <summary> Logs all activity start/stop and time and enables performance counters </summary>
		// someday: PerformanceCounts = System.Diagnostics.SourceLevels.ActivityTracing,
	}

	/// <summary>
	/// Defines the various possble outputs of the logging system
	/// </summary>
	public enum LogOutputs : ushort
	{
		/// <summary> No default destination (Event will still fire if anyone is subscribed) </summary>
		None = 0x0000,
		/// <summary> Outputs messages to the System.Diagnostics.Trace.WriteLine() method </summary>
		/// <remarks> Note: Always on by default when System.Diagnostics.Debugger.IsAttached == true </remarks>
		TraceWrite = 0x0001,
		/// <summary> Outputs messages to this process' log file </summary>
		/// <remarks> Note: Always on by default to \Users\{Current}\AppData\Local\{Process Name}\{Process File Name}.log </remarks>
		LogFile = 0x0002,
		/// <summary> Outputs messages to the Console.[Out/Error].WriteLine() methods </summary>
		/// <remarks> Note: Always on by default when running as a console application </remarks>
		Console = 0x0004,
		/// <summary> Outputs messages to the event log </summary>
		/// <remarks> Note: Always on by default for Critical errors only </remarks>
		EventLog = 0x0010,
		/// <summary> Writes to all LogOutput types available </summary>
		All = 0xFFFF
	}

	/// <summary>
	/// Performance and behavior related LogOption for the log system.
	/// </summary>
	public enum LogOptions
	{
		/// <summary>
		/// These are the default options used.  Addtionally, if your debugging or using asp.net trace,
		/// the following will also be set: LogAddFileInfo
		/// </summary>
		Default = LogImmediateCaller | GZipLogFileOnRoll,
		/// <summary>
		/// No LogOption enabled
		/// </summary>
		None = 0,
		/// <summary>
		/// Calls new StackFrame( n ) to retrieve the immediate caller of the log routine and pass 
		/// it along to all logging information.
		/// </summary>
		LogImmediateCaller = 0x0001,
		/// <summary>
		/// Starting with n frames back walk back until the calling class is not decorated with the
		/// ////[System.Diagnostics.DebuggerNonUserCode()] attribute.  This allows you to create wrapper
		/// classes that provide logging but are not considered to be the point of origin in the log.
		/// Can be slightly slower as this now reflects each class' attributes and addtionally may 
		/// gather more than one stack frame.
		/// </summary>
		LogNearestCaller = 0x0003,
		/// <summary>
		/// If this is specified the file and line number where the log call was made will be available
		/// to log LogOutput.
		/// </summary>
		LogAddFileInfo = 0x0005,

		/// <summary>
		/// Uses a different color based on the level of the log message, Use ONLY nothing else is
		/// writting to the console.
		/// </summary>
		ConsoleColors = 0x0008,

		/// <summary>
		/// Populate the MethodAssemblyVersion and MethodAssembly properties for logging. Requires
		/// at least the LogImmediateCaller information.
		/// </summary>
		LogAddAssemblyInfo = 0x0011,

		/// <summary>
		/// GZip and append a .gz extension to log files as they are rolled, the current log file
		/// will remain unzipped.
		/// </summary>
		GZipLogFileOnRoll = 0x0020,
	}
}
