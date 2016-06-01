
namespace SCPUploader
{
    public class Service : FolderMonitor.Monitor
    {
        public void StartService()
        {
            base.Start();
        }

        public void StopService()
        {
            base.Stop();
        }
    }
}
