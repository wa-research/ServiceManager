using System.Timers;
using ServiceManager;

namespace DatabasePoller
{
    public class Service
    {
        Timer _timer;

        public void StartService(ServiceContext ctx)
        {
            _timer = new Timer(15 * 1000);
            _timer.Elapsed += (s, e) => { ServiceContext.LogInfo("Timer fired"); QueryRunner.Run();  };
            _timer.Start();
            ServiceContext.LogInfo("Timer started; will query the database every 15 seconds");
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
