using System;
using System.Diagnostics;
using System.Timers;
using ServiceManager.ServiceSupport.Logging;

namespace Simpleton
{
    public class Service
    {
        Timer _timer;
        public void StartService()
        {
            Console.WriteLine("Will log to {0}", Log.Config.LogFile);
            Log.Info("Simpleton starting; will write a log line every 15 seconds.");
            _timer = new Timer(15 * 1000);
            _timer.Elapsed += (s, e) => { Log.Info("Simpleton: timer fired"); };
            _timer.Start();
        }

        public void StopService()
        {
            Log.Info("Simpleton stopping");
            if (_timer != null) {
                _timer.Stop();
                _timer.Dispose();
            }
        }
    }
}
