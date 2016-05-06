using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace FolderMonitor
{
    public class FolderMonitorConfigHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            List<WatcherInfo> config = new List<WatcherInfo>();
            foreach (XmlNode node in section.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Comment) {
                    config.Add(ReadNode(node));
                }
            }
            return config;
        }

        #endregion

        WatcherInfo ReadNode(XmlNode node)
        {
            XPathNavigator nav = node.CreateNavigator();
            XmlSerializer ser = new XmlSerializer(typeof(WatcherInfo));
            return (WatcherInfo)ser.Deserialize(new XmlNodeReader(node));
        }
    }
}

