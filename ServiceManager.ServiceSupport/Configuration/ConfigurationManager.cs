using System.Collections.Specialized;

namespace ServiceManager.ServiceSupport.Configuration
{
    public class ConfigurationManager
    {
        public static NameValueCollection AppSettings
        {
            get { return System.Configuration.ConfigurationManager.AppSettings;  }
        }
    }
}
