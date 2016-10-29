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

        private XDocument Scan(bool ScanOS = false, bool force = false)
        {
            if (DateTime.Now.Subtract(LastScan).TotalMinutes < 5 && !force)
            {
                return XDocument.Load("nmap.xml");
            }
            var sb = new StringBuilder();
            sb.Append(Subnet);
            if (ScanOS)
                sb.Append(" -O"); // Scan OS
            else
                sb.Append(" -sn"); // No Ports (faster)
            sb.Append(" -PR"); // ARP Ping
            sb.Append(" -oX nmap.xml"); // Output
            var args = sb.ToString();
            ProcessStartInfo psi;
            if (RequiresSudo)
            {
                psi = new ProcessStartInfo("sudo", $"nmap {args}")
                {
                    UseShellExecute = false
                };
            }
            else
            {
                psi = new ProcessStartInfo("nmap", args)
                {
                    UseShellExecute = false
                };
            }
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
                    case "hostscript":
                        var nbstat = ele.Elements("script").Where(s => s.Attribute("id").Value == "nbstat").FirstOrDefault();
                        if (nbstat != null)
                        {
                            var name = (from e in nbstat.Elements()
                                        where e.Name == "elem"
                                        where e.Attribute("key").Value == "server_name"
                                        select e.Value).FirstOrDefault();
                            if (!string.IsNullOrEmpty(name))
                                host.Hostname = name;                            
                        }
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
            if (!doc.Root.Elements("host").Any())
            {
                RequiresSudo = true;
                doc = Scan(false, true);
            }
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
            if (string.IsNullOrEmpty(host.Hostname))
                new TaskFactory().StartNew(new Action<object>(LookupHostname), host);

        }

        object extralock = new object();

        private void LookupHostname(object arg)
        {
            Host host = arg as Host;

            // sudo nmap 192.168.0.0/24 -oX extra.xml
            var sb = new StringBuilder();
            sb.Append(" -sU");
            sb.Append(" -p 137,5353");
            sb.Append(" --script nbstat,dns-service-discovery");
            sb.Append(" ");
            sb.Append(host.Address);
            sb.Append(" -oX extra.xml"); // Output
            var args = sb.ToString();
            ProcessStartInfo psi;
            if (RequiresSudo)
            {
                psi = new ProcessStartInfo("sudo", $"nmap {args}")
                {
                    UseShellExecute = false
                };
            }
            else
            {
                psi = new ProcessStartInfo("nmap", args)
                {
                    UseShellExecute = false
                };
            }
            lock (extralock)
            {
                var p = Process.Start(psi);
                p.WaitForExit();
                LastScan = DateTime.Now;

                var extra = XDocument.Load("extra.xml");
                foreach (var h in extra.Root.Elements("host"))
                {
                    // Just in case someone change IP addresses while we were scanning?
                    var MacAddress = h.Elements("address").FirstOrDefault(a => a.Attribute("addrtype").Value == "mac")?.Attribute("addr").Value;
                    var hostinfo = KnownHosts.FirstOrDefault(n => n.HardwareAddress == MacAddress);

                    Merge(hostinfo, h);
                }
            }
        }
    }
}
