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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServiceManager.ServiceSupport.Logging
{
	static partial class MessageQueue
	{
		static ManualResetEvent __recievingShutdown = new ManualResetEvent(true);

		static bool UseThreadedMessages = false;
		static ManualResetEvent __shutdown = new ManualResetEvent(false);

		static Thread __workerThread;
		static ManualResetEvent __queueReady = new ManualResetEvent(false);

		static readonly Object MessageSync = new Object();
		static List<EventData> Messages = new List<EventData>();

		private static bool IsRunning
		{
			get { return __shutdown.WaitOne(0, false) == false; }
			set { if(UseThreadedMessages = value) __shutdown.Reset(); else __shutdown.Set(); }
		}

		/// <summary>
		/// This thread proc manages the pumping of messages to the listeners, this is
		/// ONLY available when StartThreading() has been called via constructor of a
		/// ThreadingControl() object, (client calls Log.AppStart( name ))
		/// </summary>
		static void WorkerThreadProc()
		{
			Log.Verbose("Log.WorkerThread Started.");
			try
			{
				while(IsRunning || Messages.Count > 0)
				{
					try
					{
						if (Messages.Count == 0)
							__queueReady.WaitOne();

						List<EventData> dataList = null;
						if (Messages.Count > 0)
						{
							lock (MessageSync)
							{
								__queueReady.Reset();
								dataList = Messages;
								Messages = new List<EventData>();
							}
						}

						if (dataList == null || dataList.Count == 0)
							continue;

						PumpMessages(dataList);
					}
					catch (ThreadAbortException)
					{ return; }
					catch (Exception e)
					{
						LogUtils.LogError(e);
					}
				}//end while
			}
			finally
			{
				//this is nessessary only if someone called Thread.Abort() on us...
				IsRunning = false;
				Log.Verbose("Log.WorkerThread Exited.");
			}
		}

		/// <summary>
		/// This is NOT thread safe, locking is done through ThreadingControl
		/// </summary>
		public static void StartThreading()
		{
			//not really a need to lock the MessageSync, set running and we'll get the messages when we start...
			IsRunning = true;
			__workerThread = new Thread(WorkerThreadProc);
			__workerThread.SetApartmentState(ApartmentState.STA);
			__workerThread.Priority = ThreadPriority.BelowNormal;
			__workerThread.Name = "Log.WorkerThread";
			__workerThread.Start();
		}

		/// <summary>
		/// This is NOT thread safe, locking is done through ThreadingControl
		/// </summary>
		public static void StopThreading()
		{
			try
			{
				lock(MessageSync)
					IsRunning = false;

				__queueReady.Set();
				__workerThread.Join();
			}
			catch(Exception e) { LogUtils.LogError(e); }
			finally
			{
				__workerThread = null;
			}
		}

		public class ThreadingControl : IDisposable
		{
			private readonly string _name;
			private readonly DateTime _start;
			private bool _inuse;

			static int __openCount = 0;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public ThreadingControl(string name)
			{
				_start = DateTime.Now;
				_name = name;
				if((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose)
					MessageQueue.Push(2, LogLevels.Verbose, null, String.Format("AppStart {0}", _name), null);

				lock(typeof(ThreadingControl))
				{
					_inuse = true;
					if(1 == ++__openCount)
						StartThreading();
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			void IDisposable.Dispose()
			{
				lock(typeof(ThreadingControl))
				{	// the use of this flag prevents multiple calls to dispose from messing up the __openCount
					if(_inuse)
					{
						_inuse = false;
						if(0 == --__openCount)
							StopThreading();
					}
				}

				if((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose)
					MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("End {0} ({1} ms)", _name, (DateTime.Now - _start).TotalMilliseconds), null);
			}
		}
	}
}
