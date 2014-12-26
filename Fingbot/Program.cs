using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            slack.OnEvent += slack_OnEvent;
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

        static void slack_OnEvent(object sender, SlackEventArgs e)
        {
            if (e.Data.Type == "hello")
            {
                Console.WriteLine("Connected.");
            }
            if (e.Data is Message)
            {
                var message = e.Data as Message;
                Console.WriteLine(SubstituteMarkup(message.ToString(), sender as Slack));

                bool match =
                    message.Text.ToLower().Contains("who's in?") ||
                    message.Text.ToLower().Contains("whos in?") ||
                    message.Text.ToLower().Contains("anybody in?") ||
                    message.Text.ToLower().Contains("anyone in?");
                if (match)
                {
                    var host = Singleton<NetworkData>.Instance.PickCertainHost();
                    if (host != null)
                        (sender as Slack).SendMessage(message.Channel, String.Format("{0}'s {1} '{2}' is here...", host.Owner, host.Type, host.FriendlyName));
                    else if ((host = Singleton<NetworkData>.Instance.PickIncompleteHost()) != null)
                    {
                        (sender as Slack).SendMessage(message.Channel, String.Format("I don't know. But there is a device I don't recognise: {0}", host.FriendlyName));
                    }
                    else
                        (sender as Slack).SendMessage(message.Channel, "Nobody. It's lonely here :frowning:");

                }

            }
        }


        private static string SubstituteMarkup(string p, Slack instance)
        {
            return Regex.Replace(p, @"<([@#])(.*?)>", (match) =>
            {
                switch (match.Groups[1].Value)
                {
                    case "#":
                        var chan = instance.GetChannel(match.Groups[2].Value);
                        if (chan == null)
                            break;
                        return "#" + chan.Name;
                    case "@":
                        var user = instance.GetUser(match.Groups[2].Value);
                        if (user == null)
                            break;
                        return "@" + user.Name;

                }
                return match.Groups[0].Value;
            });
        }
    }
}
