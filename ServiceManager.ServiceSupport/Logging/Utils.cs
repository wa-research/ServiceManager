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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceManager.ServiceSupport.Logging
{
	[System.Diagnostics.DebuggerNonUserCode()]
	static class LogUtils
	{
		static readonly Regex fieldsFromFormat = new Regex("{(?<field>[a-zA-Z]+)(?:[^}]+)?}");

		static Dictionary<string, string> FormatStrings = new Dictionary<string, string>();
		internal static string PrepareFormatString(string format)
		{
			string formatting;
			if(!FormatStrings.TryGetValue(format, out formatting))
			{
				StringBuilder sbNewFormat = new StringBuilder();

				MatchCollection matches = fieldsFromFormat.Matches(format);
				int currIx = 0;
				foreach(Match match in matches)
				{
					Group found = match.Groups["field"];

					LogFields fieldIx;
					try { fieldIx = (LogFields)Enum.Parse(typeof(LogFields), found.Value, true); }
					catch { continue; }

					int start = found.Index;
					int end = found.Index + found.Length;
					string replace = ((int)fieldIx).ToString();

					char ch;
					if( EventDataFormatter.GetFormatKeyOf( fieldIx, out ch ) )
						replace = String.Format("0:!{0}", ch);

					sbNewFormat.Append(format, currIx, found.Index - currIx);
					sbNewFormat.Append(replace);

					currIx = end;
				}

				sbNewFormat.Append(format, currIx, format.Length - currIx);

				//This is nasty, we only want to do this once...
				formatting = sbNewFormat.ToString();

				try { String.Format(formatting, new object[VersionInfo.FieldCount]); }
				catch( FormatException ) { formatting = Configuration.FORMAT_DEFAULT; }
				
				//we use the this[] set operator not Add() so that multiple threads can concurrently be here, yet the
				//actual set operation must be locked to preserve the validity of the collection
				lock(FormatStrings)
					FormatStrings[format] = formatting;
			}
			return formatting;
		}

		internal static void LogError( Exception e )
		{
			if (e == null)
				return;
			if(e is System.Threading.ThreadAbortException)
				throw e;
			try { System.Diagnostics.Trace.TraceError( e.ToString() ); } catch { }
			try 
			{
				string path = Path.Combine(Path.GetDirectoryName(Configuration.CurrentLogFile), "ServiceManager.ServiceSupport.Logging.Errors.txt");
				lock (typeof(LogUtils))
				{
					int max_size = 65535;
					if (File.Exists(path) && new FileInfo(path).Length > max_size)
					{
						//we need to trim some data out of the file...
						List<string> lines = new List<string>(File.ReadAllLines(path));
						lines.RemoveRange(0, lines.Count / 2);
						File.WriteAllLines(path, lines.ToArray());
					}
				}
				StringBuilder sbDesc = new StringBuilder();
				sbDesc.AppendLine(new String('*', 80));
				sbDesc.AppendLine("Exception:");
				sbDesc.AppendLine(e.ToString());
				sbDesc.AppendLine();
				sbDesc.AppendLine("Caught in:");
				sbDesc.AppendLine(new System.Diagnostics.StackTrace(false).ToString());
				sbDesc.AppendLine();
				sbDesc.AppendLine(new String('*', 80));

				lock(typeof(LogUtils))
				{
					File.AppendAllText(path, sbDesc.ToString());
				}
			} 
			catch { }
		}

		internal static string Format( string format, object[] args )
		{
			try
			{
				if (args.Length == 0) return format;
				return String.Format(Configuration.FormatProvider, format, args);
			}
			catch (System.Threading.ThreadAbortException) { throw; }
			catch (Exception e)
			{
				LogError(e);
				try
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					sb.AppendFormat("Error formatting string '{0}' with arguments ('", format == null ? "null" : format);
					if (args == null)
						sb.Append("null");
					else if (args.Length > 0)
					{
						foreach (object o in args)
						{
							try
							{ sb.AppendFormat(Configuration.FormatProvider, "{0}', '", o); }
							catch
							{ sb.AppendFormat("{0}', '", o == null ? null : o.GetType()); }
						}
						sb.Length = sb.Length - 3;
					}
					sb.AppendFormat(") error = {0}", e.Message);
					return sb.ToString();
				}
				catch { }
				return e.ToString();
			}
		}
	}
}
