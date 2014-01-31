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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServiceManager.ServiceSupport.Logging
{
	[System.Diagnostics.DebuggerNonUserCode()]
	class TraceStack : IDisposable
	{
		[ThreadStatic]
		static List<TraceStack> __threadStack;

		readonly string _name;
		readonly DateTime _start;
		readonly int _depth;
		bool _disposed = false;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public TraceStack(string name)
		{
			if(__threadStack == null) __threadStack = new List<TraceStack>(25);

			_depth = __threadStack.Count;
			__threadStack.Add(this);

			_start = DateTime.Now;
			_name = name;

			if((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose)
				MessageQueue.Push(2, LogLevels.Verbose, null, String.Format("Start {0}", _name), null);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void IDisposable.Dispose()
		{
			if (_disposed ) return;
			_disposed = true;

			while (__threadStack != null && __threadStack.Count > _depth)
			{
				__threadStack[__threadStack.Count - 1]._disposed = true;
				__threadStack.RemoveAt(__threadStack.Count - 1);
			}

			if((Configuration.LogLevel & LogLevels.Verbose) == LogLevels.Verbose)
				MessageQueue.Push(1, LogLevels.Verbose, null, String.Format("End {0} ({1} ms)", _name, (DateTime.Now - _start).TotalMilliseconds), null);
		}

		internal static void Clear() 
		{
			if (__threadStack != null)
			{
				foreach (TraceStack ts in __threadStack)
					ts._disposed = true;
			}
			__threadStack = null; 
		}

		internal static string[] CurrentStack
		{
			get
			{
				if(__threadStack == null || __threadStack.Count == 0)
					return null;

				List<string> items = new List<string>();
				foreach(TraceStack ts in __threadStack)
					items.Add(ts._name);
				return items.ToArray();
			}
		}
	}
}
