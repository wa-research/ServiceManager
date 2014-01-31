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
using System.Runtime.Serialization;

namespace ServiceManager.ServiceSupport.Logging
{
	/// <summary>
	/// Exceptions of any kind are turned into this when serialized to another process... This is intentional to prevent issues
	/// of crossing .Net versions as well as simple client coding mistakes (Non-Serializable Exceptions).
	/// </summary>
	[Serializable]
	[System.Diagnostics.DebuggerNonUserCode()]
	class SharedException : Exception
	{
		string _message, _source, _stack, _toString;

		public SharedException(Exception rawException)
			: base(String.Empty)
		{
			if (rawException == null)
			{
			}
			else
			{
				try { _message = rawException.Message; } catch { _message = String.Format("Excpetion of type {0} was thrown.", rawException.GetType()); }
				try { _source = rawException.Source; } catch { _source = rawException.GetType().Assembly.GetName().Name; }
				try { _stack = rawException.StackTrace; } catch { _stack = String.Empty; }
				try { _toString = rawException.ToString(); } catch { _toString = String.Format("Exception: {0}{1}{2}", _message, Environment.NewLine, _stack); }
			}
		}

		public SharedException(SerializationInfo info, StreamingContext context)
			: base()
		{
			try { _message = info.GetString("se:_message"); } catch { _message = "Exception"; }
			try { _source = info.GetString("se:_source"); } catch { _source = "Unknown"; }
			try { _stack = info.GetString("se:_stack"); } catch { _stack = String.Empty; }
			try { _toString = info.GetString("se:_toString"); } catch { _toString = String.Format("Exception: {0}{1}{2}", _message, Environment.NewLine, _stack); }
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("se:_message", _message);
			info.AddValue("se:_source", _source);
			info.AddValue("se:_stack", _stack);
			info.AddValue("se:_toString", _toString);
		}

		public override string Message { get { return _message; } }
		public override string Source { get { return _source; } }
		public override string StackTrace { get { return _stack; } }
		public override string ToString() { return _toString; }
	}
}