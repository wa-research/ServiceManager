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
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceManager.ServiceSupport.Logging
{
	/// <summary>
	/// This is the class that is used to transfer log events through the system. It's basically
	/// a 'picture' of the state at the time the log was written since all logging is actually
	/// happening in a delayed fashion.
	/// </summary>
	[Serializable]
	[System.Diagnostics.DebuggerNonUserCode()]
	public sealed partial class EventData : ISerializable
	{
		#region internal EventData()
		/// <summary>
		/// Asserts that the LogFields enumeration has the same fields as this class
		/// </summary>
		static EventData()
		{
			VersionInfo.Assert();
		}

		private object[] _data;
		internal EventData(object[] values)
		{
			_data = values;
			_data[0] = this;

			//These are used heavily so they are extracted now.
			Level = (LogLevels)_data[(int)LogFields.Level];
			Output = (LogOutputs)_data[(int)LogFields.Output];
		}
		#endregion

		#region Formattable Properties

		/// <summary> A unique integer representing the index the event was written since program start </summary>
		public int EventId { get { return (int)_data[(int)LogFields.EventId]; } } 
		/// <summary> Returns the time when the log event was raised </summary>
		public DateTime EventTime { get { return (DateTime)_data[(int)LogFields.EventTime]; } } 

		/// <summary> The current process id </summary>
		public Int32 ProcessId { get { return (Int32)_data[(int)LogFields.ProcessId]; } } 
		/// <summary> The current process name </summary>
		public String ProcessName { get { return (String)_data[(int)LogFields.ProcessName]; } } 
		/// <summary> The current app domain's friendly name </summary>
		public String AppDomainName { get { return (String)_data[(int)LogFields.AppDomainName]; } } 
		/// <summary> The current app domain's entry-point assembly name </summary>
		public String EntryAssembly { get { return (String)_data[(int)LogFields.EntryAssembly]; } } 
		/// <summary> The logging thread's 'CurrentPrincipal.Identity.Name' property </summary>
		public String ThreadPrincipalName { get { return (String)_data[(int)LogFields.ThreadPrincipalName]; } } 

		/// <summary> The managed thread id that called the log routine </summary>
		public Int32 ManagedThreadId { get { return (Int32)_data[(int)LogFields.ManagedThreadId]; } } 
		/// <summary> The managed thread name (if any) or String.Empty </summary>
		public String ManagedThreadName { get { return (String)_data[(int)LogFields.ManagedThreadName]; } } 
		
		/// <summary> The LogLevel of the log data or LogLevels.None if Log.Write() was called </summary>
		public readonly LogLevels Level;
		/// <summary> The outputs that should recieve this message </summary>
		public readonly LogOutputs Output;
		/// <summary> An instance of the Exception class or null if none provided </summary>
		public Exception Exception { get { return (Exception)_data[(int)LogFields.Exception]; } } 
		/// <summary> The formatted string of the message or String.Empty if none </summary>
		public String Message { get { return (String)_data[(int)LogFields.Message]; } } 
		
		/// <summary> The file name where the log was called from or null if no file dataList available/configured. </summary>
		public String FileName { get { return (String)_data[(int)LogFields.FileName]; } } 
		/// <summary> The file line number where the log was called from. </summary>
		public int FileLine { get { return (int)_data[(int)LogFields.FileLine]; } } 
		/// <summary> The file column where the log was called from. </summary>
		public int FileColumn { get { return (int)_data[(int)LogFields.FileColumn]; } } 
		
		/// <summary> The assembly's version that contained the method where the log was called from. </summary>
		public String MethodAssemblyVersion { get { return (String)_data[(int)LogFields.MethodAssemblyVersion]; } }
		/// <summary> The assembly's name that contained the method where the log was called from. </summary>
		public String MethodAssembly { get { return (String)_data[(int)LogFields.MethodAssembly]; } }
		/// <summary> The type containing the method where the log was called from. </summary>
		public String MethodType { get { return (String)_data[(int)LogFields.MethodType]; } }
		/// <summary> The unqualified type containing the method where the log was called from. </summary>
		public String MethodTypeName { get { return (String)_data[(int)LogFields.MethodTypeName]; } } 

		/// <summary> The method where the log was called from. </summary>
		public String MethodName { get { return (String)_data[(int)LogFields.MethodName]; } } 
		/// <summary> The method's argument names and types. </summary>
		public String MethodArgs { get { return (String)_data[(int)LogFields.MethodArgs]; } } 
		/// <summary> Returns the IL offset within the calling method </summary>
		public int IlOffset { get { return (int)_data[(int)LogFields.IlOffset]; } }

		/// <summary> returns the text given the most recent call to Log.Start() that has not yet been Disponsed</summary>
		public string LogCurrent { get { string[] stack = (string[])_data[(int)LogFields.LogStack]; return stack != null && stack.Length > 0 ? stack[stack.Length-1] : null; } }
		/// <summary> returns the text given to all calls to Log.Start() that has not yet been Disponsed separated by '::' </summary>
		public string LogStack { get { if(_data[(int)LogFields.LogStack] == null) return null; return String.Join("::", (string[])_data[(int)LogFields.LogStack]); } }

		/// <summary>
		/// The full method information: Type.Name(Args)
		/// </summary>
		public string Location { get { return String.Format(Configuration.FormatProvider, Configuration.FORMAT_LOCATION, _data); } }

		/// <summary>
		/// The full method information: Type.Name(Args)
		/// </summary>
		public string Method { get { return _data[(int)LogFields.MethodType] == null ? null : String.Format(Configuration.FormatProvider, Configuration.FORMAT_METHOD, _data); } }

		/// <summary>
		/// Returns the full message of Message + Location + Exception
		/// </summary>
		public string FileLocation { get { return _data[(int)LogFields.FileName] == null ? null : String.Format(Configuration.FormatProvider, Configuration.FORMAT_FILELINE, _data); } }

		/// <summary>
		/// Returns the full message of Message + Location + Exception
		/// </summary>
		public string FullMessage { get { return String.Format(Configuration.FormatProvider, Configuration.FORMAT_FULLMESSAGE, _data); } }
		
		#endregion

		#region Internal-Use-Only

		/// <summary>
		/// Used for string formatting, the order of these MUST match the order of names return by FieldNames
		/// </summary>
		internal object[] ToObjectArray() { return _data; }

		#endregion

		#region ToString/Write/ToXml

		/// <summary>
		/// Displays this log data in a default brief format
		/// </summary>
		public override string ToString()
		{
			return string.Format(Configuration.InnerFormatter, "[{0:D2}] {1,8} - {2}", 
				_data[(int)LogFields.ManagedThreadId], _data[(int)LogFields.Level], _data[(int)LogFields.Message]);
		}

		/// <summary>
		/// Displays this log data in a specific format.  Use '{FieldName}' to be substituded with it's value.
		/// Sorry but it uses a case-sensitive match of fields in this class.
		/// </summary>
		public string ToString(string format)
		{
			try { return String.Format(Configuration.FormatProvider, LogUtils.PrepareFormatString(format), ToObjectArray()); }
			catch(Exception e) { LogUtils.LogError(e); }
			return Message;
		}

		/// <summary>
		/// Writes this event to the text writter with the default brief format
		/// </summary>
		public void Write(System.IO.TextWriter writer)
		{
			try { writer.WriteLine(String.Format(Configuration.FormatProvider, Configuration.FORMAT_DEFAULT, ToObjectArray())); }
			catch(Exception e) { LogUtils.LogError(e); }
		}

		/// <summary>
		/// Writes this log data in a specific format.  Use '{FieldName}' to be substituded with it's value.
		/// Sorry but it uses a case-sensitive match of fields in this class.
		/// </summary>
		public void Write(System.IO.TextWriter writer, string format)
		{
			try { writer.WriteLine(String.Format(Configuration.FormatProvider, LogUtils.PrepareFormatString(format), ToObjectArray())); }
			catch(Exception e) { LogUtils.LogError(e); }
		}

		/// <summary>
		/// Writes a custom xml format for this record.
		/// </summary>
		public void Write(System.Xml.XmlWriter writer)
		{
			try
			{
				writer.WriteStartElement("e");
				try
				{
					writer.WriteAttributeString("id", _data[(int)LogFields.EventId].ToString());
					writer.WriteAttributeString("time", ((DateTime)_data[(int)LogFields.EventTime]).ToString("u").TrimEnd('Z'));
					writer.WriteAttributeString("pid", _data[(int)LogFields.ProcessId].ToString());
					writer.WriteAttributeString("tid", _data[(int)LogFields.ManagedThreadId].ToString());
					writer.WriteAttributeString("lvl", _data[(int)LogFields.Level].ToString());
					if(_data[(int)LogFields.Message] != null)
						writer.WriteAttributeString("msg", _data[(int)LogFields.Message].ToString());
					if(_data[(int)LogFields.Exception] != null)
						writer.WriteAttributeString("err", _data[(int)LogFields.Exception].ToString());

					if(_data[(int)LogFields.MethodType] != null)
					{
						writer.WriteAttributeString("type", _data[(int)LogFields.MethodType].ToString());
						writer.WriteAttributeString("method", _data[(int)LogFields.MethodName].ToString());
						writer.WriteAttributeString("args", _data[(int)LogFields.MethodArgs].ToString());
						writer.WriteAttributeString("il", _data[(int)LogFields.IlOffset].ToString());
					}
					if(_data[(int)LogFields.FileName] != null)
					{
						writer.WriteAttributeString("file", _data[(int)LogFields.FileName].ToString());
						writer.WriteAttributeString("line", _data[(int)LogFields.FileLine].ToString());
						writer.WriteAttributeString("col", _data[(int)LogFields.FileColumn].ToString());
					}
					string[] stack = _data[(int)LogFields.LogStack] as string[];
					if( stack != null && stack.Length > 0 )
						writer.WriteAttributeString("stack", stack[stack.Length-1]);
				}
				finally { writer.WriteEndElement(); }
			}
			catch(Exception e)
			{ LogUtils.LogError(e); }
		}

		/// <summary>
		/// Returns this data as an XmlWriter text fragment who name is the class name and each attribute
		/// is the field name, again maintaining the case.
		/// </summary>
		public string ToXml()
		{
			System.IO.StringWriter sw = new System.IO.StringWriter();
			System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(sw);
			writer.Formatting = System.Xml.Formatting.Indented;
			Write(writer);
			writer.Flush();
			return sw.ToString();
		}

		#endregion

		#region Custom Serialization

		/// <summary> /// Serialization Constructor for ISerializable /// </summary>
		public EventData(SerializationInfo info, StreamingContext context)
		{
			string sver = info.GetString("rec:ver");
			if (sver != String.Format("{0}:{1}", VersionInfo.FieldCount, VersionInfo.CheckSum))
				throw new SerializationException();

			_data = new object[VersionInfo.FieldCount];
			Type t;

			Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (SerializationEntry entry in info)
				values.Add(entry.Name, entry.Value);

			for (int i = 0; i < _data.Length; i++)
			{
				try
				{
					if (i == (int)LogFields.Exception && values.ContainsKey("hasError") && (bool)values["hasError"])
						_data[i] = new SharedException(info, context);

					object value;
					if (null != (t = VersionInfo.FieldTypes[i]) && values.TryGetValue(i.ToString(), out value))
						_data[i] = value;
				}
				catch (Exception e) { System.Diagnostics.Trace.WriteLine(e.ToString()); }
			}

			//These are used heavily so they are extracted now.
			_data[0] = this;
			_data[(int)LogFields.Output] = this.Output = Configuration.LogOutput;
			_data[(int)LogFields.Level] = Level = (LogLevels)_data[(int)LogFields.Level];
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("rec:ver", String.Format("{0}:{1}", VersionInfo.FieldCount, VersionInfo.CheckSum));
			Type t;
			for (int i = 0; i < _data.Length; i++)
			{
				try
				{
					if (i == (int)LogFields.Exception && _data[i] is Exception)
					{ new SharedException(_data[i] as Exception).GetObjectData(info, context); info.AddValue("hasError", true); }
					else
						if (null != (t = VersionInfo.FieldTypes[i]) && null != _data[i])
						{
							object value = _data[i];
							if (value.GetType() != t)
								value = Convert.ChangeType(value, t);
							info.AddValue(i.ToString(), value, t);
						}
				}
				catch (Exception e) { System.Diagnostics.Trace.WriteLine(e.ToString()); }
			}
		}
		#endregion
	}
}
