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
using System.Security.Permissions;
using System.Diagnostics;

using ServiceManager.ServiceSupport.Logging;
using System.Runtime.CompilerServices;

namespace ServiceManager.ServiceSupport.Logging
{
    /// <summary>
    /// Provides an abstraction api for logging to various outputs.  This class in the global namespace for a reason.
    /// Since you may want to change log infrastructure at any time, it's important that no 'Using' statements are
    /// required to access the log infrastructure.  Secondly, a static class api can provide improved performance
    /// over other possible types.  We will be forcing the default configuration of log4net rather than requiring
    /// each component to independently configure itself.
    /// </summary>
    [System.Reflection.Obfuscation(Exclude = true)]
    [System.Diagnostics.DebuggerNonUserCode()]
    public static partial class Log
    {
        static Log()
        {
            Configuration.Configure();
        }
        #region Config Options
        /// <summary>
        /// Provides configuration options for the Log subsystem
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode()]
        public static class Config
        {
            static Config()
            {
                //force the static c'tor on Log()
                Log.IsVerboseEnabled.ToString();
            }

            /// <summary> Gets or sets the current log LogLevel </summary>
            public static LogLevels Level
            {
                get { return Configuration.LogLevel; }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set
                {
                    if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("Log Level changed from {0} to {1}.", Configuration.LogLevel, value), null);
                    Configuration.LogLevel = value;
                }
            }
            /// <summary> Gets or sets the current log LogLevel </summary>
            public static LogOutputs Output
            {
                get { return Configuration.LogOutput; }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set
                {
                    if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("Log Output changed from {0} to {1}.", Configuration.LogOutput, value), null);
                    Configuration.LogOutput = value;
                }
            }
            /// <summary> 
            /// Gets or sets the availability of stack info etc.  The following performance characterists were taken with a 
            /// simple example app running 10 threads each writing 1000 log statements in a tight-loop.  The times below reflect
            /// threading enabled (someone having called Log.AppStart()).  The following hardware was used:
            /// AMD Turion 64x2 TL-62  2.10 GHz, 3 GB RAM running Vista 32-bit SP1  (HP Notebook).  The time indicated below
            /// was estimated by taking the total number of milliseconds until all threads completed and dividing by the 10,000
            /// messages that were written.
            /// <list>
            ///		<item>Cost per call 0.005 ms with no context   (None)</item>
            ///		<item>Cost per call 0.035 ms with calling method   (LogImmediateCaller)</item>
            ///		<item>Cost per call 0.060 ms with calling method &amp; assembly info   (LogImmediateCaller | LogAddAssemblyInfo)</item>
            ///		<item>Cost per call 0.160 ms with calling method &amp; file info   (LogImmediateCaller | LogAddFileInfo)</item>
            ///		<item>Cost per call 0.190 ms with calling method &amp; assembly info &amp; file info   (LogImmediateCaller | LogAddAssemblyInfo | LogAddFileInfo)</item>
            /// </list>
            /// </summary>
            public static LogOptions Options
            {
                get { return Configuration.LogOption; }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set
                {
                    if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("Log Options changed from {0} to {1}.", Configuration.LogOption, value), null);
                    Configuration.LogOption = value;
                }
            }

            /// <summary> Gets or sets the format provider to use when formatting strings </summary>
            public static IFormatProvider FormatProvider { set { if (value != null) Configuration.InnerFormatter = value; } }

            /// <summary> Changes the log level required to write to a specific output device. </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static LogLevels SetOutputLevel(LogOutputs outputToConfigure, LogLevels newLevel)
            {
                LogLevels old = LogLevels.None;
                switch (outputToConfigure) {
                    case LogOutputs.Console: { old = Configuration.LEVEL_CONSOLE; Configuration.LEVEL_CONSOLE = newLevel; break; }
                    case LogOutputs.EventLog: { old = Configuration.LEVEL_EVENTLOG; Configuration.LEVEL_EVENTLOG = newLevel; break; }
                    case LogOutputs.LogFile: { old = Configuration.LEVEL_LOGFILE; Configuration.LEVEL_LOGFILE = newLevel; break; }
                    case LogOutputs.TraceWrite: { old = Configuration.LEVEL_TRACE; Configuration.LEVEL_TRACE = newLevel; break; }
                    case LogOutputs.All: {
                            Configuration.LEVEL_CONSOLE = newLevel;
                            Configuration.LEVEL_EVENTLOG = newLevel;
                            Configuration.LEVEL_LOGFILE = newLevel;
                            Configuration.LEVEL_TRACE = newLevel;
                            break;
                        }
                }
                MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("Log level for {0} changed from {1} to {2}.", outputToConfigure, old, newLevel), null);
                return old;
            }

            /// <summary> 
            /// Changes the output format used to write to a specific output device. The format of this string behaves just like
            /// EventData.ToString().  The string can contain any public field or property available for the ServiceManager.ServiceSupport.Logging.EventData 
            /// class surrounded by braces {} and yes, properties/fields are case sensative.  The input string should look something
            /// like the following examples: 
            /// "[{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}" -- this is the default format of ToString()
            /// "{EventTime:o} [{ProcessId:D4},{ManagedThreadId:D2}] {Level,8} - {Message}{Location}{Exception}"  -- This is the default log file format.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SetOutputFormat(LogOutputs outputToConfigure, string newFormat)
            {
                newFormat = LogUtils.PrepareFormatString(newFormat);
                switch (outputToConfigure) {
                    case LogOutputs.Console: { Configuration.FORMAT_CONSOLE = newFormat; break; }
                    case LogOutputs.EventLog: { Configuration.FORMAT_EVENTLOG = newFormat; break; }
                    case LogOutputs.LogFile: { Configuration.FORMAT_LOGFILE = newFormat; break; }
                    case LogOutputs.TraceWrite: { Configuration.FORMAT_TRACE = newFormat; break; }
                    case LogOutputs.All: {
                            Configuration.FORMAT_CONSOLE = newFormat;
                            Configuration.FORMAT_EVENTLOG = newFormat;
                            Configuration.FORMAT_LOGFILE = newFormat;
                            Configuration.FORMAT_TRACE = newFormat;
                            break;
                        }
                }
                MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("Log output for {0} changed to: '{1}'.", outputToConfigure, newFormat), null);
            }

            /// <summary>
            /// Gets or sets the current log file name, insert '{0}' in the file's name to allow log rolling
            /// </summary>
            public static string LogFile
            {
                get { return Configuration.CurrentLogFile; }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set
                {
                    string newname = value;
                    try {
                        if (!System.IO.Path.IsPathRooted(newname))
                            newname = System.IO.Path.GetFullPath(newname);
                        Configuration.CurrentLogFile = new System.IO.FileInfo(newname).FullName;
                        MessageQueue.LogFileChanged();
                    } catch (ArgumentException ae) {
                        MessageQueue.Push(1, LogLevels.Error, ae, String.Format("Unable to set log path to: {0}", value), null);
                    }
                }
            }

            /// <summary> Gets or sets the maximum size in bytes the log file is allowed to be before rolling to history </summary>
            public static int LogFileMaxSize { get { return Configuration.FILE_SIZE_THREASHOLD; } set { Configuration.FILE_SIZE_THREASHOLD = Math.Max(8192, value); } }
            /// <summary> Gets or sets the maximum number of history log files to keep on the system </summary>
            public static int LogFileMaxHistory { get { return Configuration.FILE_MAX_HISTORY_SIZE; } set { Configuration.FILE_MAX_HISTORY_SIZE = Math.Max(1, value); } }

            /// <summary> Sets the event log name we will write events to. </summary>
            public static string EventLogName { get { return Configuration.EventLogName; } set { Configuration.EventLogName = value; } }
            /// <summary> Sets the event log source we will write events with, It's up to you to register this value. </summary>
            public static string EventLogSource { get { return Configuration.EventLogSource; } set { Configuration.EventLogSource = value; } }
        }

        #endregion

        /// <summary> Returns true if 'Info' messages are being recorded. </summary>
        public static bool IsInfoEnabled { get { return (Configuration.LogLevel & LogLevels.Info) == LogLevels.Info; } }
        /// <summary> Returns true if 'Verbose' messages are being recorded. </summary>
        public static bool IsVerboseEnabled { get { return (Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose; } }

        // FYI, these are only for the benifit of the following methods...
        private static readonly Exception e = null;
        private static readonly string format = String.Empty;
        private static readonly object[] args = new object[0];

        /// <summary> 
        /// Enables calls to Log.xxx() to be processed on another thread to improve throughput... 
        /// Place this in a using() statement within Main:  using( new Log.AppStart("Some Name") )
        /// This call (and Disponse) IS Thread safe and can be called multiple times either 
        /// concurrently, sequentially, nested, or overlapping calls are all permitted and handled.
        /// </summary>
        public static IDisposable AppStart(string format, params object[] args) { return new MessageQueue.ThreadingControl(LogUtils.Format(format, args)); }

        /// <summary>
        /// Pushes a string into the trace stack so that log messages appear with the 'context'
        /// set to the information provided.  The operation named should be performed by the
        /// current thread and the IDisposable object returned should be disposed when the
        /// operation completes.
        /// </summary>
        /// <example>
        /// using( Log.Start("Reading File") )
        /// {
        ///		... do something ...
        /// }
        /// </example>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>An IDisposable object that should be destroyed by calling the Dispose() 
        /// method when the activity is complete.</returns>
        public static IDisposable Start(string format, params object[] args) { return new TraceStack(LogUtils.Format(format, args)); }

        /// <summary>
        /// Forces any left-behind calls to Start() to be closed.
        /// </summary>
        public static void ClearStack() { TraceStack.Clear(); }

        /// <summary>
        /// Write directly to the log reguardless of the currently configured log-LogLevel
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Write(string format, params object[] args) { MessageQueue.Push(1, LogLevels.None, e, format, args); }

        /// <summary> Logs a Critical error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Critical(Exception e) { if ((Configuration.LogLevel & LogLevels.Critical) == LogLevels.Critical) MessageQueue.Push(1, LogLevels.Critical, e, format, args); }
        /// <summary> Logs a Critical error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Critical(Exception e, string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Critical) == LogLevels.Critical) MessageQueue.Push(1, LogLevels.Critical, e, format, args); }
        /// <summary> Logs a Critical error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Critical(string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Critical) == LogLevels.Critical) MessageQueue.Push(1, LogLevels.Critical, e, format, args); }

        /// <summary> Logs an Error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(Exception e) { if ((Configuration.LogLevel & LogLevels.Error) == LogLevels.Error) MessageQueue.Push(1, LogLevels.Error, e, format, args); }
        /// <summary> Logs an Error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(Exception e, string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Error) == LogLevels.Error)  MessageQueue.Push(1, LogLevels.Error, e, format, args); }
        /// <summary> Logs an Error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Error) == LogLevels.Error) MessageQueue.Push(1, LogLevels.Error, e, format, args); }

        /// <summary> Logs a Warning </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(Exception e) { if ((Configuration.LogLevel & LogLevels.Warning) == LogLevels.Warning) MessageQueue.Push(1, LogLevels.Warning, e, format, args); }
        /// <summary> Logs a Warning </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(Exception e, string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Warning) == LogLevels.Warning) MessageQueue.Push(1, LogLevels.Warning, e, format, args); }
        /// <summary> Logs a Warning </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Warning) == LogLevels.Warning) MessageQueue.Push(1, LogLevels.Warning, e, format, args); }

        /// <summary> Logs a Info error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Info(Exception e) { if ((Configuration.LogLevel & LogLevels.Info) == LogLevels.Info) MessageQueue.Push(1, LogLevels.Info, e, format, args); }
        /// <summary> Logs a Info error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Info(Exception e, string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Info) == LogLevels.Info) MessageQueue.Push(1, LogLevels.Info, e, format, args); }
        /// <summary> Logs an Informational message </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Info(string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Info) == LogLevels.Info) MessageQueue.Push(1, LogLevels.Info, e, format, args); }

        /// <summary> Logs a Verbose error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Verbose(Exception e) { if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, e, format, args); }
        /// <summary> Logs a Verbose error </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Verbose(Exception e, string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, e, format, args); }
        /// <summary> Logs a Verbose message </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Verbose(string format, params object[] args) { if ((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose) MessageQueue.Push(1, LogLevels.Verbose, e, format, args); }
    }
}
