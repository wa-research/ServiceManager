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
    public static partial class Log
    {
        /// <summary>
        /// This event is raised when the logging routines are called and the LogLevel is at or
        /// higher than the current Log.LogLevels field value.
        /// </summary>
        public static event LogEventHandler LogWrite
        {
            add { lock (MessageQueue.ListenerSync) MessageQueue.InternalLogWrite += value; }
            remove { lock (MessageQueue.ListenerSync) MessageQueue.InternalLogWrite -= value; }
        }
    }

    /// <summary>
    /// Log events are raised with this event handler
    /// </summary>
    public delegate void LogEventHandler(object sender, LogEventArgs args);

    /// <summary>
    /// The event args passed to the log system, often multiple log entries will arrive 
    /// 'near' simultaneous, ALWAYs allow for ZERO or more than one.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerNonUserCode()]
    public sealed class LogEventArgs : System.EventArgs, ISerializable, IEnumerable<EventData>
    {
        List<EventData> _data;

        /// <summary> /// Serialization Constructor for ISerializable /// </summary>
        public LogEventArgs(SerializationInfo info, StreamingContext context)
        {
            _data = new List<EventData>();
            int count = info.GetInt32("Count");
            for (int i = 0; i < count; i++) {
                EventData item = (EventData)info.GetValue(i.ToString(), typeof(EventData));
                _data.Add(item);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Count", _data.Count);
            for (int i = 0; i < _data.Count; i++)
                info.AddValue(i.ToString(), _data[i], typeof(EventData));
        }

        /// <summary>
        /// Constructs a list event log dataList to be provided the Log.LogWrite event
        /// </summary>
        internal LogEventArgs(List<EventData> data)
        { _data = data; }

        /// <summary>
        /// Returns the count of items in the collection
        /// </summary>
        public int Count { get { return _data.Count; } }
        /// <summary>
        /// Returns the items as an array of EventData
        /// </summary>
        public EventData[] ToArray() { return _data.ToArray(); }
        /// <summary>
        /// Returns the entire collection of EventData records as a single line-delimited string
        /// </summary>
        public override string ToString()
        {
            if (_data.Count == 0)
                return String.Empty;
            if (_data.Count == 1)
                return _data[0].ToString();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.IO.StringWriter sw = new System.IO.StringWriter(sb);
            foreach (EventData ed in _data) {
                ed.Write(sw);
                sw.WriteLine();
            }
            sw.Flush();
            //trims the last \r\n when complete
            return sb.ToString(0, sb.Length - sw.NewLine.Length);
        }

        /// <summary>
        /// Returns the enumeration of the EventData structures
        /// </summary>
        public IEnumerator<EventData> GetEnumerator() { return _data.GetEnumerator(); }

        /// <summary>
        /// Returns the non-generic version of the enumeration
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return ((System.Collections.IEnumerable)_data).GetEnumerator(); }
    }

}
