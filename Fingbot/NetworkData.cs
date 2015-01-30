using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Net;

namespace Fingbot
{
    internal class NetworkData
    {
        private List<Host> KnownHosts;

        public NetworkData()
        {
            KnownHosts = Serialization.TryReadObject<List<Host>>("Hosts.json") ?? new List<Host>();
            var fing = Process.GetProcessesByName("fing");
            if (fing.Length == 0)
            {
                try{
                Process.Start("fing", PersistentSingleton<Settings>.Instance.FingArgs);
                if (string.IsNullOrEmpty(PersistentSingleton<Settings>.Instance.FingArgs))
                    PersistentSingleton<Settings>.Dirty();
                }
                catch(System.IO.FileNotFoundException)
                {
                    Console.WriteLine("WARNING: Fing not Installed!");
                }

            }
        }

        public Host PickIncompleteHost()
        {
            KnownHosts.Sort(SortRandom);
            foreach (var host in KnownHosts)
            {
                if (!string.IsNullOrWhiteSpace(host.Owner) || !string.IsNullOrWhiteSpace(host.Type))
                    continue;
                if (host.State == "down")
                    continue;
                if (string.IsNullOrEmpty(host.Hostname))
                    continue;
                return host;
            }
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
                if (prop.Name == "LastChangeTime") // Override this one every time.
                    prop.SetValue(host, key.Value, null);
            }
            if (string.IsNullOrEmpty(host.Vendor))
                new TaskFactory().StartNew(new Action<object>(LookupVendor), host);
        }

        private void LookupVendor(object state)
        {
            Host host = state as Host;
            if (host == null)
                return;
            WebClient wc = new WebClient();
            var data = wc.DownloadString(string.Format("http://www.macvendorlookup.com/api/v2/{0}", host.HardwareAddress));
            var json = Newtonsoft.Json.Linq.JArray.Parse(data);
            host.Vendor = json[0]["company"].ToString();

        }

        internal Host PickCertainHost()
        {
            KnownHosts.Sort(SortRandom);
            foreach (var host in KnownHosts)
            {
                if (string.IsNullOrWhiteSpace(host.Owner))
                    continue;
                if (host.State == "down")
                    continue;
                if (host.IsFixture)
                    continue;
                if (host.IsOld)
                    continue;
                //if (host.LastChangeTime
                return host;
            }
            return null;

        }

        private int SortRandom(Host x, Host y)
        {
            return Singleton<Random>.Instance.Next(-1, 1);
        }
    }
}