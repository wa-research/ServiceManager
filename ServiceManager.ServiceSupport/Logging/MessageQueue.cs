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
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace ServiceManager.ServiceSupport.Logging
{
	/// <summary>
	/// NOTE TO READER:
	/// 
	/// I will appologize up front for this library.  I don't normally condone this type of proceedural
	/// and repetitive coding.  This is ONLY being done in this library to reduce the temporal cost of
	/// using it.  
	/// In a nutshell: this being easy to maintain is not as important as library performance.
	/// </summary>
	[System.Diagnostics.DebuggerNonUserCode()]
	static partial class MessageQueue
	{
		/// <summary> This allows for async subscriptions to InternalLogWrite event </summary>
		public static readonly Object ListenerSync = new Object();
		/// <summary> This is the actual event list we will dispatch to when logs are written </summary>
		public static event LogEventHandler InternalLogWrite;

		/// <summary> Keeps a running counter of events and assigns a unique one to each log entry</summary>
		private static int __eventCounter;
		/// <summary> If logging configured to disk, we manage the currently open log file here</summary>
		static MessageLogFile _logFile = null;
		/// <summary> Used to syncronize access to log file while writting log entries and changing the log file</summary>
		static readonly object LogFileSync = new object();

		public static void Push(int depth, LogLevels level, Exception error, string format, object[] args)
		{
			try
			{
				#region Gather all the information...

				DateTime time = DateTime.Now;
				System.Diagnostics.StackFrame frame;
				System.Reflection.MethodBase method;
				bool getFrame = (Configuration.LogOption & LogOptions.LogImmediateCaller) == LogOptions.LogImmediateCaller;
				bool getFile = (Configuration.LogOption & LogOptions.LogAddFileInfo) == LogOptions.LogAddFileInfo;

				string message = format;
				string[] stack = null;
				System.Security.Principal.IPrincipal currUser = null;
				AssemblyName asmName = null;
				Type methodType = null;
				string filename = null, methodName = null, methodArgs = null;
				int lineNo = 0, lineCol = 0, ilOffset = 0;
				
				try
				{
					message = (args != null && args.Length > 0) ? LogUtils.Format(format, args) : format;
					if(String.IsNullOrEmpty(message) && error != null)
						message = error.Message;

					if(getFrame && GetStack(1 + depth, getFile, out frame, out method))
					{
						methodType = method.ReflectedType;
						if( (Configuration.LogOption & LogOptions.LogAddAssemblyInfo) == LogOptions.LogAddAssemblyInfo )
							asmName = methodType.Assembly.GetName();
						methodName = method.Name;
						methodArgs = FormatArgs(method.GetParameters());
						ilOffset = frame.GetILOffset();

						if(getFile && null != (filename = frame.GetFileName()))
						{
							lineNo = frame.GetFileLineNumber();
							lineCol = frame.GetFileColumnNumber();
						}
						else
							lineNo = lineCol = 0;
					}

					stack = TraceStack.CurrentStack;
					currUser = System.Threading.Thread.CurrentPrincipal;
				}
				catch(Exception e) { LogUtils.LogError(e); }

				#endregion
				#region Push all the crap into a great big object dump...

				object[] properties = new object[VersionInfo.FieldCount];
				
				properties[(int)LogFields.EventId] = Interlocked.Increment( ref __eventCounter );
				properties[(int)LogFields.EventTime] = time;

				properties[(int)LogFields.ProcessId] = Configuration.ProcessId;
				properties[(int)LogFields.ProcessName] = Configuration.ProcessName;
				properties[(int)LogFields.AppDomainName] = Configuration.AppDomainName;
				properties[(int)LogFields.EntryAssembly] = Configuration.EntryAssembly;
				properties[(int)LogFields.ThreadPrincipalName] = currUser == null ? null : currUser.Identity.Name;

				properties[(int)LogFields.ManagedThreadId] = Thread.CurrentThread.ManagedThreadId;
				properties[(int)LogFields.ManagedThreadName] = Thread.CurrentThread.Name;

				properties[(int)LogFields.Level] = level;
				properties[(int)LogFields.Output] = Configuration.LogOutput;
				properties[(int)LogFields.Exception] = error;
				properties[(int)LogFields.Message] = message;
				
				properties[(int)LogFields.FileName] = filename;
				properties[(int)LogFields.FileLine] = lineNo;
				properties[(int)LogFields.FileColumn] = lineCol;

				properties[(int)LogFields.MethodAssemblyVersion] = asmName == null ? null : asmName.Version.ToString();
				properties[(int)LogFields.MethodAssembly] = asmName == null ? null : asmName.Name;
				properties[(int)LogFields.MethodType] = methodType == null ? null : methodType.FullName;
				properties[(int)LogFields.MethodTypeName] = methodType == null ? null : methodType.Name;

				properties[(int)LogFields.MethodName] = methodName;
				properties[(int)LogFields.MethodArgs] = methodArgs;
				properties[(int)LogFields.IlOffset] = ilOffset;

				//expressions are left null: Location, Method, FullMessage, etc... The exceptions to this follow:
				properties[(int)LogFields.LogStack] = stack;

				#endregion
				#region Now create and dispatch the event record...

				EventData data = new EventData(properties);
				
				if(Configuration.IsDebugging && (Configuration.LogOutput & LogOutputs.TraceWrite) == LogOutputs.TraceWrite
					&& (Configuration.LEVEL_TRACE & data.Level) == data.Level)
				{//	When debugging & stepping through code this is useful to not delay

					System.Diagnostics.Trace.WriteLine(String.Format(Configuration.FormatProvider, Configuration.FORMAT_TRACE, properties), data.MethodType);
				}

				int queueSizeCurrent = 0;

				//now queue it up so everyone else can get it...
				lock( MessageSync )
				{
					if(UseThreadedMessages)
					{
						Messages.Add(data);
						__queueReady.Set();
						queueSizeCurrent = Messages.Count;
					}
					else
					{
						//until the AppStart or after it's disposal we need to dispatch on thread
						List<EventData> dataList = new List<EventData>(1);
						dataList.Add(data);
						PumpMessages(dataList);
					}
				}

				//Just in case something bad is happening downstream, we need to slow up a bit and let it do some chatch-up...
				if (queueSizeCurrent > 10000)//everyone pays the price ... this should really NEVER happen if you where actually doing something besides calling Log.Write()
					Thread.Sleep(queueSizeCurrent / 100);
				else if (queueSizeCurrent > 5000 && queueSizeCurrent % 25 == 0)//one in every 25 calls slows after 1000 backlog
					Thread.Sleep(100);
				else if (queueSizeCurrent > 1000 && queueSizeCurrent % 100 == 0)//one in every 100 calls slows after 1000 backlog
					Thread.Sleep(100);

				#endregion
			}
			catch( Exception e )
			{
				LogUtils.LogError( e );
			}
		}

		static void PumpMessages(List<EventData> dataList)
		{
			if(dataList == null || dataList.Count == 0)
				return;

			// now that we have some dataList, dispatch to known outputs
			foreach(EventData data in dataList)
			{
				object[] args = data.ToObjectArray();
				#region Trace Output
				//Trace if not debugging (already written on Push if we are debugging)
				if(Configuration.IsDebugging == false && (data.Output & LogOutputs.TraceWrite) == LogOutputs.TraceWrite
					&& (Configuration.LEVEL_TRACE & data.Level) == data.Level)
					System.Diagnostics.Trace.WriteLine(String.Format(Configuration.FormatProvider, Configuration.FORMAT_TRACE, args), data.MethodType);
				#endregion
				#region Console Output
				if((data.Output & LogOutputs.Console) == LogOutputs.Console
					&& (Configuration.LEVEL_CONSOLE & data.Level) == data.Level)
				{
					ConsoleColor orig = Console.ForegroundColor;
					try
					{
						TextWriter output = Console.Out;
						ConsoleColor color = Console.ForegroundColor;
						if(data.Level == LogLevels.Error || data.Level == LogLevels.Critical)
						{
							color = ConsoleColor.Red;
							output = Console.Error;
						}
						else if(data.Level == LogLevels.Warning)
							color = ConsoleColor.Yellow;
						else if(data.Level == LogLevels.Info)
							color = ConsoleColor.White;
						else
							color = ConsoleColor.Gray;

						if((Configuration.LogOption & LogOptions.ConsoleColors) == LogOptions.ConsoleColors)
							Console.ForegroundColor = color;
						output.WriteLine(String.Format(Configuration.FormatProvider, Configuration.FORMAT_CONSOLE, args));
					}
					catch(Exception e) { LogUtils.LogError(e); }
					finally
					{
						if((Configuration.LogOption & LogOptions.ConsoleColors) == LogOptions.ConsoleColors)
							Console.ForegroundColor = orig;
					}
				}
				#endregion
				#region Log File Output
				if((data.Output & LogOutputs.LogFile) == LogOutputs.LogFile
					&& (Configuration.LEVEL_LOGFILE & data.Level) == data.Level)
				{
					lock (LogFileSync)
					{
						// check to see if we can still use the current log file
						if (_logFile != null && _logFile.IsOpen)
						{
							try
							{
								//has the size exceeded max?
								if (_logFile.TextWriter.BaseStream.Position > Configuration.FILE_SIZE_THREASHOLD)
									_logFile.Dispose();//need a new file...

								//has the configuration changed?
								if (_logFile.CurrentFile != Configuration.CurrentLogFile)
									_logFile.Dispose();
							}
							catch (ObjectDisposedException)
							{ _logFile.Dispose(); }
						}

						//open a new log file if needed
						if (_logFile == null || false == _logFile.IsOpen)
							MessageLogFile.OpenFile(Configuration.CurrentLogFile, ref _logFile);

						//if all is well, log to file...
						if (_logFile != null && _logFile.IsOpen)
						{
							try
							{
								if (_logFile.IsXmlFile)
								{
									_logFile.TextWriter.WriteLine(data.ToXml());
									//keep the end of root written in the file so the xml is always valid
									long pos = _logFile.TextWriter.BaseStream.Position;
									_logFile.TextWriter.WriteLine("</log>");
									_logFile.TextWriter.BaseStream.Position = pos;
								}
								else
									_logFile.TextWriter.WriteLine(String.Format(Configuration.FormatProvider, Configuration.FORMAT_LOGFILE, args));
							}
							catch (ObjectDisposedException e)
							{ _logFile.Dispose(); LogUtils.LogError(e); }
							catch (System.IO.IOException e)
							{ _logFile.Dispose(); LogUtils.LogError(e); }
							catch (Exception e)
							{ LogUtils.LogError(e); }
						}
					}
				}
				#endregion
				#region Event Log Writer
				if((data.Output & LogOutputs.EventLog) == LogOutputs.EventLog
					&& (Configuration.LEVEL_EVENTLOG & data.Level) == data.Level)
				{
					//we will only pass a selective set to the event log... write through, warning and higher
					if(EventLogSource.IsWorking)
					{
						int eventId;
						try
						{
							EventLog log = EventLogSource.GetLogFor(data, out eventId);
							if(log != null)
								log.WriteEntry(
								String.Format(Configuration.FormatProvider, Configuration.FORMAT_EVENTLOG, args),
								data.Level == LogLevels.Error || data.Level == LogLevels.Critical ? EventLogEntryType.Error :
								data.Level == LogLevels.Warning ? EventLogEntryType.Warning : EventLogEntryType.Information,
								eventId);

							EventLogSource.IsWorking = true;
						}
						catch(Exception e)
						{
							if(EventLogSource.IsWorking)
								LogUtils.LogError(e);
							EventLogSource.IsWorking = false;
						}
					}
				}
				#endregion
			}

			LogEventArgs evntArgs = new LogEventArgs(dataList);
			LogEventHandler handler = InternalLogWrite;
			if(handler != null)
			{
				foreach(LogEventHandler h in handler.GetInvocationList())
				{
					try
					{
						h(null, evntArgs);
					}
					catch(ThreadAbortException)
					{ return; }
					catch(Exception e)
					{ LogUtils.LogError(e); }
				}
			}
		}

		static bool GetStack(int depth, bool getFile, out StackFrame frame, out MethodBase method)
		{
			method = null;
			frame = null;

			if((Configuration.LogOption & LogOptions.LogImmediateCaller) == LogOptions.LogImmediateCaller)
			{
				depth += 1;//and one for me...
				frame = new StackFrame( depth, getFile );

				if(frame != null && (Configuration.LogOption & LogOptions.LogNearestCaller) == LogOptions.LogNearestCaller)
				{
					method = frame.GetMethod();
					while (method != null && (
						method.ReflectedType.IsDefined(typeof(System.Diagnostics.DebuggerStepThroughAttribute), false) ||
						method.ReflectedType.IsDefined(typeof(System.Diagnostics.DebuggerNonUserCodeAttribute), false)))
					{
						method = null;
						if( null != (frame = new StackFrame( ++depth, getFile )) )
							method = frame.GetMethod();
					}
				}
				else
					method = frame.GetMethod();
			}
			return frame != null && method != null;
		}

		static string FormatArgs(ParameterInfo[] args)
		{
			if( args == null || args.Length == 0 )
				return String.Empty;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach( ParameterInfo arg in args )
				sb.AppendFormat( "{0} {1}, ", arg.ParameterType.Name, arg.Name );

			return sb.ToString(0, sb.Length - 2);
		}

		public static void LogFileChanged()
		{
			lock (LogFileSync)
			{
				if (_logFile != null && _logFile.CurrentFile != Configuration.CurrentLogFile)
				{
					_logFile.Dispose();
					_logFile = null;
				}
			}
		}
	}
}
