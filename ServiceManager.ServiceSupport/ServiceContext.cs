using ServiceManager.ServiceSupport.Logging;

namespace ServiceManager
{
    public class ServiceContext
    {
        public string ServiceName { get; set; }
        public static Logger Trace { get; set; }

        static Logger _logger;
             
        internal static void SetLogger(Logger logger)
        {
            _logger = logger;
        }

        public static void Log(int eventId, string message, params object[] args)
        {
            if (_logger != null)
                _logger.LogEvent(eventId, message, args);
        }
         
        public static void LogWarning(string message, params object[] args)
        {
            Log(Logger.EventNumber.Warning, message, args);
        }

        public static void LogInfo(string message, params object[] args)
        {
            Log(Logger.EventNumber.Information, message, args);
        }
    }
}
