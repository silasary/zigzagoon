using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text;
using System.Net.NetworkInformation;

namespace Fingbot
{
    internal class NetworkData
    {
        private IScanner Tool;

        private List<Host> KnownHosts;

        public Host[] AllHosts { get { return KnownHosts.ToArray(); } }

        public NetworkData()
        {
            KnownHosts = Serialization.TryReadObject<List<Host>>("Hosts.json") ?? Serialization.TryReadObject<List<Host>>("Hosts.Backup.json") ?? new List<Host>();
            Serialization.WriteObject("Hosts.Backup.json", KnownHosts);

            IScanner tool = new Scanners.Nmap(KnownHosts);
            if (tool.IsValidTool())
            {
                Tool = tool;
                return;
            }
            tool = new Scanners.Fing(KnownHosts);
            if (tool.IsValidTool())
            {
                Tool = tool;
                return;
            }
            
            Console.WriteLine("WARNING: No network scanner installed! \n> Please install NMap or Overlook Fing");
        }

        public Host PickIncompleteHost()
        {
            KnownHosts.Sort(SortRandom);
            foreach (var host in KnownHosts) // First time, we avoid devices with no known name
            {
                if (!string.IsNullOrWhiteSpace(host.Owner) || !string.IsNullOrWhiteSpace(host.Type))
                    continue;
                if (host.State == "down")
                    continue;
                if (string.IsNullOrEmpty(host.Hostname))
                    continue;
                if (host.IsFixture)
                    continue;
                return host;
            }
            foreach (var host in KnownHosts) // Second time, we will show nameless devices.
            {
                if (!string.IsNullOrWhiteSpace(host.Owner) || !string.IsNullOrWhiteSpace(host.Type))
                    continue;
                if (host.State == "down")
                    continue;
                if (host.IsFixture)
                    continue;
                return host;
            }
            return null;
        }

        public void Refresh()
        {
            Tool.Refresh();
            Save();
        }

        public void Save()
        {
            try
            {
                Serialization.WriteObject("Hosts.json", KnownHosts);
            }
            catch (IOException c)
            {
                Console.WriteLine("Error writing to disk:\n {0}", c);
            }
         }

        public static void LookupVendor(object state)
        {
            try {
                Host host = state as Host;
                if (host == null)
                    return;
                WebClient wc = new WebClient();
                var data = wc.DownloadString(string.Format("http://www.macvendorlookup.com/api/v2/{0}", host.HardwareAddress));
                var json = Newtonsoft.Json.Linq.JArray.Parse(data);
                host.Vendor = json[0]["company"].ToString();
            }
            catch (Exception c)
            {

            }
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

        internal IEnumerable<Host> CertainHosts()
        {
            //KnownHosts.Sort();

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

                yield return host;
            }
            yield break;
        }

        public IEnumerable<Host> UnknownHosts()
        {
            foreach (var host in KnownHosts)
            {
                if (!string.IsNullOrWhiteSpace(host.Owner) || !string.IsNullOrWhiteSpace(host.Type))
                    continue;
                if (host.State == "down")
                    continue;
                yield return host;
            }
            yield break;
        }

        private int SortRandom(Host x, Host y)
        {
            return Singleton<Random>.Instance.Next(-1, 1);
        }

        internal string Status(Host host)
        {
            if (host.State == "down")
                return "Offline";
            if (host.IsFixture)
                return "Ignored as fixture";
            if (host.IsOld)
                return string.Format("Too old ({0} hours)", host.Age);
            //if (host.LastChangeTime
            if (string.IsNullOrEmpty(host.Owner))
                return "Unowned";
            return string.Format("Owned by {0}", host.Owner);
        }

        internal Host Find(string p)
        {
            return AllHosts.FirstOrDefault(
                n =>
                    p.Equals(n.Name, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Equals(n.Hostname, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Equals(n.HardwareAddress, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Equals(n.Address, StringComparison.CurrentCultureIgnoreCase)
            );
        }
    }
}