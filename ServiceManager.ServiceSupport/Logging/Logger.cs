using System;
using System.Diagnostics;

namespace ServiceManager.ServiceSupport.Logging
{
    public class Logger
    {
        TraceSource _ts;
        internal Logger(TraceSource ts)
        {
            _ts = ts;
        }

        internal static Logger Create(string name)
        {
            var ts = new TraceSource(name);
            ts.Listeners.Clear();
            ts.Listeners.Add(new ConsoleTraceListener());
            ts.Switch.Level = SourceLevels.All;

            return new Logger(ts);
        }

        public void LogEvent(int eventId, string message, params object[] args)
        {
            _ts.TraceEvent(EventTypeFromId(eventId), eventId, message, args);
        }

        #region Event numbers
        // http://essentialdiagnostics.codeplex.com/wikipage?title=Event%20Ids&referringTitle=Guidance
        private TraceEventType EventTypeFromId(int eventId)
        {
            switch (eventId / 1000) {
                case 1:
                case 2:
                case 3:
                case 8:
                    return TraceEventType.Information;
                case 4:
                    return TraceEventType.Warning;
                case 5:
                    return TraceEventType.Error;
                case 9:
                    return TraceEventType.Critical;
                default:
                    return TraceEventType.Information;
            }
        }

        public static class EventNumber
        {
            // Informational
            public const int Information = 1000;

            public const int ServiceManagerStarting = 1001;
            public const int ServiceManagerStarted = 2001;
            public const int ServiceManagerStopping = 8001;
            public const int ServiceManagerStopped = 8002;

            public const int ServieLoading = 1002;
            public const int ServiceLoaded = 2002;
            public const int ServiceStarting = 1003;
            public const int ServiceStarted = 1004;

            public const int ServiceStopping = 8003;
            public const int ServiceStopped = 8004;

            public const int Warning = 4000;

            public const int Error = 5000;
            public const int ServiceFailedToLoad = 5001;
            public const int ServiceFailedToStart = 5002;
        }
        #endregion
    }
}
