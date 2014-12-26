using Kamahl.Common;
using SlackRTM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Fingbot
{
    class Program
    {
        
        static void Main(string[] args)
        {
            PersistentSingleton<Settings>.SavePath = "config.json";
            var settings = PersistentSingleton<Settings>.Instance;

            Singleton<NetworkData>.Instance.Refresh();
            var missing = Singleton<NetworkData>.Instance.PickIncompleteHost();
            Singleton<NetworkData>.Instance.Save();
            var slack = Singleton<Slack>.Instance;
            bool Authed = false;
            do
            {
                if (String.IsNullOrEmpty(settings.Token))
                {
                    Console.WriteLine("Please obtain a Token, and paste it below:");
                    settings.Token = Console.ReadLine();
                    PersistentSingleton<Settings>.Dirty();
                }
                Authed = slack.Init(settings.Token);
                if (!Authed)
                    settings.Token = "";

            } while (Authed == false);
            Console.WriteLine("Connecting...");
            slack.Connect();

            bool Running = true;
            while (Running)
            {
                if (!slack.Connected)
                {
                    Console.WriteLine("Reconnecting...");
                    slack.Init(settings.Token);
                    slack.Connect();
                }
                Thread.Sleep(1000);
                




            }
        }
    }
}
