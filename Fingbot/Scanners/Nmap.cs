using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fingbot.Scanners
{
    class Nmap : IScanner
    {
        private List<Host> KnownHosts;

        DateTime LastScan;
        private readonly string Subnet;

        public bool RequiresSudo { get; private set; }

        public Nmap(List<Host> knownHosts)
        {
            this.KnownHosts = knownHosts;

            
            var networks = (from i in NetworkInterface.GetAllNetworkInterfaces()
                where i.OperationalStatus == OperationalStatus.Up
                where i.NetworkInterfaceType == NetworkInterfaceType.Ethernet || i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                select i).ToArray();
            Subnet = networks.FirstOrDefault()?.GetIPProperties().UnicastAddresses.First(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).CalculateNetwork().ToString() + "/24";
            Console.WriteLine($"NMap range: {Subnet}");
        }

        public bool IsValidTool()
        {
            try
            {
                Scan();
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            { }
            return false;
        }

        private XDocument Scan(bool ScanOS = false)
        {
            if (DateTime.Now.Subtract(LastScan).TotalMinutes < 5)
            {
                return XDocument.Load("nmap.xml");
            }
            var sb = new StringBuilder();
            sb.Append(Subnet);
            if (ScanOS)
                sb.Append(" -O"); // Scan OS
            else
                sb.Append(" -sn"); // No Ports (faster)
            //sb.Append(" -PR"); // ARP Ping
            sb.Append(" -oX nmap.xml"); // Output

            var psi = new ProcessStartInfo("nmap", sb.ToString())
            {
                UseShellExecute = false
            };
            var p = Process.Start(psi);
            p.WaitForExit();
            LastScan = DateTime.Now;
            return XDocument.Load("nmap.xml");
        }

        public void Merge(Host host, XElement data)
        {
            foreach (var ele in data.Elements())
            {
                switch (ele.Name.LocalName)
                {
                    case "status":
                        host.State = ele.Attribute("state").Value;
                        break;
                    case "address":
                        var type = ele.Attribute("addrtype").Value;
                        switch (type)
                        {
                            case "ipv4":
                                host.Address = ele.Attribute("addr").Value;
                                break;
                            case "mac":
                                host.HardwareAddress = ele.Attribute("addr").Value;
                                break;
                            default:
                                break;
                        }
                        break;
                    case "hostnames":
                        var hname = ele.Elements("hostname").Select(e => e.Attribute("name").Value).FirstOrDefault();
                        if (!string.IsNullOrEmpty(hname))
                            host.Hostname = hname;
                        break;
                    case "times":
                        // Don't care.
                        break; 
                    default:
                        break;
                }
            }
            
        }

        public void Refresh()
        {
            var doc = Scan();
            foreach (var host in doc.Root.Elements("host"))
            {
                if (host.Element("status").Attribute("reason").Value == "localhost-response")
                    continue;
                var MacAddress = host.Elements("address").FirstOrDefault(a => a.Attribute("addrtype").Value == "mac")?.Attribute("addr").Value;
                if (MacAddress == null && !RequiresSudo)
                {
                    RequiresSudo = true;
                    Refresh();
                    return;
                }
                var hostinfo = KnownHosts.FirstOrDefault(n => n.HardwareAddress == MacAddress);
                if (hostinfo == null)
                {
                    hostinfo = new Host();
                    KnownHosts.Add(hostinfo);
                }
                Merge(hostinfo, host);
                GetDetailedInfo(hostinfo);
            }
        }

        private void GetDetailedInfo(Host host)
        {
            if (string.IsNullOrEmpty(host.Vendor))
                new TaskFactory().StartNew(new Action<object>(NetworkData.LookupVendor), host);

        }
    }
}
