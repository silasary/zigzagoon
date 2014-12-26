using Kamahl.Common;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Fingbot
{
    internal class NetworkData
    {
        private List<Host> KnownHosts;

        public NetworkData()
        {
            KnownHosts = Serialization.TryReadObject<List<Host>>("Hosts.json") ?? new List<Host>();
        }

        public Host PickIncompleteHost()
        {
            foreach (var host in KnownHosts)
            {
                if (!string.IsNullOrWhiteSpace(host.Owner) || !string.IsNullOrWhiteSpace(host.Type))
                    continue;
                if (host.State == "down")
                    continue;
                return host;
            }
            return null;
        }

        public void Refresh()
        {
            XDocument doc = XDocument.Load(PersistentSingleton<Settings>.Instance.FingXml);
            foreach (var host in doc.Root.Element("Hosts").Elements())
            {
                var MacAddress = host.Element("HardwareAddress").Value;
                var obj = KnownHosts.FirstOrDefault(n => n.HardwareAddress == MacAddress);
                Merge(obj, host);
            }
            Save();
        }

        public void Save()
        {
            Serialization.WriteObject("Hosts.json", KnownHosts);
        }

        private void Merge(Host host, XElement data)
        {
            if (host == null)
            {
                host = new Host();
                KnownHosts.Add(host);
            }
            foreach (var key in data.Elements())
            {
                var prop = typeof(Host).GetProperties().FirstOrDefault(n => n.Name == key.Name);
                if (prop != null && !string.IsNullOrEmpty(key.Value))
                {
                    prop.SetValue(host, key.Value, null);
                }
            }
        }

        internal Host PickCertainHost()
        {
            foreach (var host in KnownHosts)
            {
                if (string.IsNullOrWhiteSpace(host.Owner))
                    continue;
                if (host.State == "down")
                    continue;
                if (host.IsFixture)
                    continue;
                return host;
            }
            return null;

        }
    }
}