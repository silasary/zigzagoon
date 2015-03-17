using Kamahl.Common;
using Mono.Zeroconf.Providers.Bonjour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fingbot
{
    class ZeroConfClient
    {
        public static void Main()
        {
            string confdir;
            Directory.CreateDirectory(confdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fingbot"));
            Environment.CurrentDirectory = confdir;
            PersistentSingleton<Settings>.SavePath = "config.json";
            var zc = new ZeroConfClient(Singleton<NetworkData>.Instance);

        }


        NetworkData Network;
        ServiceBrowser Browser;
        public ZeroConfClient(NetworkData network)
        {
            this.Network = network;
            Browser = new ServiceBrowser();
            Browser.ServiceAdded += Browser_ServiceAdded;
            //Browser.Browse(0, Mono.Zeroconf.AddressProtocol.Any,);
        }

        void Browser_ServiceAdded(object o, Mono.Zeroconf.ServiceBrowseEventArgs args)
        {
            throw new NotImplementedException();
        }


    }
}
