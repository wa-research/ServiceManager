using System.Diagnostics;

namespace ServiceManager.ServiceSupport.Logging
{
    public class Tracing
    {
        public static TraceSource GetTraceSource(string name)
        {
            var ts = new TraceSource(name);
            ts.Listeners.Clear();
            ts.Listeners.Add(new ConsoleTraceListener());
            ts.Switch.Level = SourceLevels.All;

            return ts;
        }
    }
}
