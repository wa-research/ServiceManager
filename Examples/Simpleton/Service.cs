﻿using System.Diagnostics;
using System.Timers;
using ServiceManager.ServiceSupport.Logging;

namespace Simpleton
{
    public class Service
    {
        Timer _timer;
        TraceSource _logger;
        public void StartService()
        {
            _logger = Tracing.GetTraceSource("Simpleton");

            _logger.TraceInformation("Starting; will write a log line every 15 seconds.");
            _timer = new Timer(15 * 1000);
            _timer.Elapsed += (s, e) => { _logger.TraceInformation("Timer fired"); };
            _timer.Start();
        }

        public void StopService()
        {
            _logger.TraceInformation("Stopping");
            if (_timer != null) {
                _timer.Stop();
                _timer.Dispose();
            }
        }
    }
}
