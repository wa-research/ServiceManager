using System;

namespace ServiceManager
{
    class ServiceInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Path { get; set; }
        public DateTime LastModified { get; set; }
        public ServiceProxy Proxy { get; set; }
        public AppDomain AppDomain { get; set; }
    }
}
