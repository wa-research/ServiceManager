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
using System.Xml;
using System.IO.Compression;

namespace ServiceManager.ServiceSupport.Logging
{
	[System.Diagnostics.DebuggerNonUserCode()]
	class MessageLogFile : IDisposable
	{
		public readonly string CurrentFile;
		public readonly StreamWriter TextWriter;
		public readonly bool IsXmlFile = false;
		public bool IsOpen = false;

		private bool _attemptReopen = true;

		public static void OpenFile(string name, ref MessageLogFile logFile)
		{
			if(logFile != null && logFile._attemptReopen == false && name == logFile.CurrentFile)
				return;

			if(logFile != null) ((IDisposable)logFile).Dispose();
			logFile = null;

			logFile = new MessageLogFile(name);
		}

		private MessageLogFile(string name)
		{
			CurrentFile = name;

			try
			{
				IsXmlFile = StringComparer.OrdinalIgnoreCase.Equals(".xml", Path.GetExtension(name));
				OpenTextFile(name, IsXmlFile, ref TextWriter);
				IsOpen = true;
			}
			catch (Exception e)
			{
				_attemptReopen = false;
				TextWriter = null;

				if (e is IOException && e.Message.Contains("used by another process"))
				{
					try
					{
						string ext = String.Format(".{0}{1}", Configuration.ProcessId, Path.GetExtension(name));
						name = Path.ChangeExtension(name, ext);
						OpenTextFile(name, IsXmlFile, ref TextWriter);
						IsOpen = true;

						LogUtils.LogError(new System.IO.IOException(String.Format("The log file '{0}' was in use, logging to: '{1}'", CurrentFile, name)));
					}
					catch
					{ }
				}
				if (TextWriter == null || !IsOpen)
					LogUtils.LogError(e);
			}
		}

		private static void OpenTextFile(string currentFile, bool isXml, ref StreamWriter TextWriter)
		{
			//Attempt to roll the files...
			string actualFile = String.Format(currentFile, 0);
			if (File.Exists(actualFile) && (isXml || new FileInfo(actualFile).Length > Configuration.FILE_SIZE_THREASHOLD))
			{
				string rollingFile = currentFile;
				if((Configuration.LogOption & LogOptions.GZipLogFileOnRoll) == LogOptions.GZipLogFileOnRoll)
				{
					GZipLogFile(actualFile, actualFile + ".gz");
					rollingFile += ".gz";
				}

				if(null == RollingRenameFile(rollingFile, 0))
					throw new ApplicationException("Unable to roll log file(s): " + currentFile);

				if(File.Exists(actualFile))
				{ File.WriteAllText(actualFile, String.Empty); }
			}

			//Open for logging...
			if(!Directory.Exists(Path.GetDirectoryName(actualFile)))
				Directory.CreateDirectory(Path.GetDirectoryName(actualFile));

			if (isXml)
			{
				TextWriter = new StreamWriter(File.Open(actualFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read));
				TextWriter.AutoFlush = true;

				TextWriter.WriteLine("<log>");
				long pos = TextWriter.BaseStream.Position;
				TextWriter.WriteLine("</log>");
				TextWriter.BaseStream.Position = pos;
			}
			else
			{
				TextWriter = new StreamWriter(File.Open(actualFile, FileMode.Append, FileAccess.Write, FileShare.Read | FileShare.Delete));
				TextWriter.AutoFlush = true;

				//for existing text logs, write something to deliniate the new log start
				if(TextWriter.BaseStream.Position > 0)
				{
					String delimiter = new string('*', 80);
					for(int i = 0; i < 5; i++)
						TextWriter.WriteLine(delimiter);
				}
			}
		}

		public void Dispose()
		{
			if(IsOpen)
			{
				try
				{
					IsOpen = false;
					if(TextWriter != null)
						try { TextWriter.Dispose(); }
						catch(Exception e) { LogUtils.LogError(e); }
				}
				catch(ObjectDisposedException) { }
				catch(Exception e) { LogUtils.LogError(e); }
			}
		}

		private static void GZipLogFile(string from, string to)
		{
			using(Stream sout = File.Open(to, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				using(GZipStream output = new GZipStream(sout, CompressionMode.Compress, false))
				{
					using(Stream sin = File.Open(from, FileMode.Open, FileAccess.Read, FileShare.Write))
					{
						byte[] buffer = new byte[8192];
						int len;

						while(0 != (len = sin.Read(buffer, 0, buffer.Length)))
							output.Write(buffer, 0, len);
						
						output.Flush();
					}
				}
			}
		}

		private static string RollingRenameFile(string path, int new_number)
		{
			string myFile;
			try
			{
				if(path.IndexOf("{0}") < 0)//static named file...
				{
					myFile = path;
					File.Delete(myFile);
					return myFile;
				}

				if(new_number >= Configuration.FILE_MAX_HISTORY_SIZE)
					return null;

				myFile = String.Format(path, new_number);
				if(!File.Exists(myFile))
					return myFile;

				string nextFile = RollingRenameFile(path, 1 + new_number);
				if(nextFile == null)
				{
					try { File.Delete(myFile); }
					catch(Exception e) { LogUtils.LogError(e); return null; }
					return myFile;
				}
				File.Move(myFile, nextFile);
			}
			catch(Exception e) { LogUtils.LogError(e); return null; }
			return myFile;
		}

	}
}
