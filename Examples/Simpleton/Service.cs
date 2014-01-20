using System;
using System.Diagnostics;
using System.Timers;

namespace Simpleton
{
    public class Service
    {
        Timer _timer;
        public void StartService()
        {
            Log("Simpleton starting; will write a log line every 15 seconds.");
            _timer = new Timer(15 * 1000);
            _timer.Elapsed += (s, e) => { Log("Simpleton: timer fired"); };
            _timer.Start();
        }

        public void StopService()
        {
            Log("Simpleton stopping");
            if (_timer != null) {
                _timer.Stop();
                _timer.Dispose();
            }
        }

        static void Log(string message)
        {
            Log("{0}", message);
        }

        public static void Log(string format, params object[] args)
        {
            int thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            int process = Process.GetCurrentProcess().Id;

            string meta = string.Format("{0} [{1}:{2}] ", DateTime.UtcNow.ToString("s"), process, thread);
            Console.WriteLine(meta + format, args);
        }
    }
}
