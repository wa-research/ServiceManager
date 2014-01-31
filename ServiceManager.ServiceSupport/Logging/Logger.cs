using System;

namespace ServiceManager.ServiceSupport.Logging
{
    public class Logger
    {
        public static void Critical(Exception e) { }
        public static void Critical(Exception e, string format, params object[] args) { }
        public static void Critical(string format, params object[] args) { }
        public static void Critical(Action logMessageGenerator) { }

        public static void Error(Exception e) { }
        public static void Error(Exception e, string format, params object[] args) { }
        public static void Error(string format, params object[] args) { }
        public static void Error(Action logMessageGenerator) { }

        public static void Warning(Exception e) { }
        public static void Warning(Exception e, string format, params object[] args) { }
        public static void Warning(string format, params object[] args) { }
        public static void Warning(Action logMessageGenerator) { }

        public static void Info(Exception e) { }
        public static void Info(Exception e, string format, params object[] args) { }
        public static void Info(string format, params object[] args) { }
        public static void Info(Action logMessageGenerator) { }

        public static void Verbose(Exception e) { }
        public static void Verbose(Exception e, string format, params object[] args) { }
        public static void Verbose(string format, params object[] args) { }
        public static void Verbose(Action logMessageGenerator) { }
    }
}
