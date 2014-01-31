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
using System.Diagnostics;
using System.Security;

namespace ServiceManager.ServiceSupport.Logging
{
	[System.Diagnostics.DebuggerNonUserCode()]
	static class EventLogSource
	{
		public static bool IsWorking = true;
		static EventLog __log = null;

		public static EventLog GetLogFor(EventData data, out int eventId)
		{ 
			if( __log == null || __log.Log != Configuration.EventLogName || __log.Source != Configuration.EventLogSource )
			{
				try
				{
					__log = null;
					__log = new EventLog(Configuration.EventLogName);
					__log.Source = Configuration.EventLogSource;
				}
				catch(Exception e) { LogUtils.LogError(e); }
			}
			eventId = 0; 
			return __log; 
		}
	}
}

static class Unknown { }