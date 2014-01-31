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
using System.Text;

namespace ServiceManager.ServiceSupport.Logging
{
	[System.Diagnostics.DebuggerNonUserCode()]
	sealed class EventDataFormatter : IFormatProvider, ICustomFormatter
	{
		public object GetFormat(Type service)
		{
			if(service == typeof(ICustomFormatter))
				return this;
			return null;
		}

		public static bool GetFormatKeyOf( LogFields field, out char chKey )
		{
			chKey = '\0';
			if(field == LogFields.Location) chKey = 'L';
			if(field == LogFields.Method) chKey = 'M';
			if(field == LogFields.FileLocation) chKey = 'f';
			if(field == LogFields.FullMessage) chKey = 'F';
			if(field == LogFields.LogStack) chKey = 'S';
			if(field == LogFields.LogCurrent) chKey = 's';
			return chKey != '\0';
		}

		public string Format(string input, object arg, IFormatProvider provider)
		{
			if(arg is Exception)
			{
				StringBuilder sbErr = new StringBuilder();
				sbErr.AppendFormat("{0}EXCEPTION {1}{0}", Environment.NewLine, new String('*', 69));
				try { sbErr.AppendLine(arg.ToString()); }
				catch { sbErr.AppendLine(arg.GetType().ToString()); }
				sbErr.AppendLine(new String('*', 79));
				return sbErr.ToString();
			}
			else if(arg is int && !String.IsNullOrEmpty(input) && input[0] == '?')
			{
				if(((int)arg) > 0)
					return ((int)arg).ToString();
				return String.Empty;
			}
			else if(arg is string && !String.IsNullOrEmpty(input))
			{
				int pos = input.IndexOf("%s", StringComparison.Ordinal);
				if(pos >= 0)
					return String.Format("{0}{1}{2}", input.Substring(0, pos), arg, input.Substring(pos + 2));
				return input;
			}
			else if(String.IsNullOrEmpty(input) || !(arg is EventData))
			{
				if(arg == null) return String.Empty;
				if(arg is IFormattable)
					return ((IFormattable)arg).ToString(input, Configuration.InnerFormatter);
				return String.Format(Configuration.InnerFormatter, "{0}", arg);
			}

			if(input[0] != '!' || input.Length < 2)
				return String.Empty;

			EventData data = (EventData)arg;
			string result = null;

			switch(input[1])
			{
				case 'L': result = data.Location; break;
				case 'M': result = data.Method; break;
				case 'f': result = data.FileLocation; break;
				case 'F': result = data.FullMessage; break;
				case 'S': result = data.LogStack; break;
				case 's': result = data.LogCurrent; break;
			}

			if(input.Length > 2 && input[2] == ':')
				return this.Format(input.Substring(3), result, provider);

			if(result != null)
				return result;

			return String.Empty;
		}
	}
}
