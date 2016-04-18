using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fingbot.Scanners
{
    class Fing : IScanner
    {
        private List<Host> KnownHosts;

        public Fing(List<Host> knownHosts)
        {
            this.KnownHosts = knownHosts;
        }

        public bool IsValidTool()
        {
            var fing = Process.GetProcessesByName("fing");
            if (fing.Length == 0)
            {
                try
                {
                    var settings = PersistentSingleton<Settings>.Instance;
                    if (string.IsNullOrEmpty(settings.FingXml))
                        settings.FingXml = "fing.xml";
                    if (string.IsNullOrEmpty(settings.FingArgs))
                        settings.FingArgs = /*"--session data.dat " + */ "-o table,xml,fing.xml -o table,csv,console";

                    Process.Start("fing", settings.FingArgs);
                    if (string.IsNullOrEmpty(settings.FingArgs))
                        PersistentSingleton<Settings>.Dirty();
                    return true;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public void Merge(Host host, XElement data)
        {
            if (host == null)
            {
                host = new Host();
                KnownHosts.Add(host);
            }
            foreach (var key in data.Elements())
            {
                var prop = typeof(Host).GetProperties().FirstOrDefault(n => n.Name == key.Name);
                if (prop == null)
                    continue;
                if (prop != null && !string.IsNullOrEmpty(key.Value))
                {
                    prop.SetValue(host, key.Value, null);
                }
                if (prop.Name == "LastChangeTime") // Override this one every time.
                    prop.SetValue(host, key.Value, null);
            }
            if (string.IsNullOrEmpty(host.Vendor))
                new TaskFactory().StartNew(new Action<object>(NetworkData.LookupVendor), host);
            if (!string.IsNullOrEmpty(host.Owner) && !host.Owner.StartsWith("@") && host.Owner != "ADB")
                host.Owner = "@" + host.Owner;
        }

        public void Refresh()
        {
            while (!System.IO.File.Exists(PersistentSingleton<Settings>.Instance.FingXml))
            {
                Console.WriteLine("Waiting for FingXml to be written.");
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 10));
            }
            XDocument doc = XDocument.Load(PersistentSingleton<Settings>.Instance.FingXml);
            foreach (var hostele in doc.Root.Element("Hosts").Elements())
            {
                var MacAddress = hostele.Element("HardwareAddress").Value;
                var hostinfo = KnownHosts.FirstOrDefault(n => n.HardwareAddress == MacAddress);
                Merge(hostinfo, hostele);
            }
        }
    }
}
