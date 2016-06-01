using System.Timers;
using ServiceManager;
using ServiceManager.ServiceSupport.Configuration;

namespace DatabasePoller
{
    public class Service
    {
        Timer _timer;

        public void StartService(ServiceContext ctx)
        {
            var interval = ConfigurationManager.AppSettings["QueryInterval"];
            int parsedInterval;
            int minutes = 2;
            if (int.TryParse(interval, out parsedInterval)) {
                minutes = parsedInterval;
            }
            _timer = new Timer(minutes * 60 * 1000);
            _timer.Elapsed += (s, e) => { ServiceContext.LogInfo("Timer fired"); QueryRunner.Run();  };
            _timer.Start();
            ServiceContext.LogInfo("Timer started; will query the database every {0} minutes", minutes);
        }

        public void StopService()
        {
            if (_timer != null) {
                _timer.Stop();
                _timer.Dispose();
            }
        }
    }
}
