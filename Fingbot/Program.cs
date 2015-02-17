using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Fingbot
{
    class Program
    {
        static bool Running;
        static void Main(string[] args)
        {
            string confdir;
            Directory.CreateDirectory(confdir= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fingbot"));
            Environment.CurrentDirectory = confdir;
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
            Running = true;
            
            int attempts = 0;
            while (Running)
            {
                if (!slack.Connected)
                {
                    try
                    {
                        Console.WriteLine("Reconnecting...");
                        slack.Init(settings.Token);
                        slack.Connect();
                        attempts = 0;
                    }
                    catch (WebException c)
                    {
                        Console.WriteLine(c);
                        Thread.Sleep(new TimeSpan(0, attempts++, 1)); // Longer wait, because something's wrong.
                    }
                }
                Thread.Sleep(1000);
            }
        }

        static void slack_OnEvent(object sender, SlackEventArgs e)
        {
            var instance = sender as Slack;
            var network = Singleton<NetworkData>.Instance;
            if (e.Data.Type == "hello")
            {
                Console.WriteLine("Connected.");
            }
            if (e.Data is Message)
            {
                network.Refresh();
                var message = e.Data as Message;
                if (message.Hidden)
                    return;
                Console.WriteLine(SubstituteMarkup(message.ToString(), sender as Slack));
                bool targeted = message.Text.StartsWith(string.Concat("@", instance.Self.Name));
                if (message.Channel[0] == 'D')
                    targeted = true;
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
                    LastHost = host;
                    return;
                }
                /* ****
                 * That's my Something
                 * ****/
                var pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    @"That's (?<Owner>a|my|the|(?<un>@[\w]+)'s) (?<Type>\w+)?", 
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    if (LastHost == null)
                    {
                        instance.SendMessage(message.Channel,  String.Format("@{0}: I have no idea what you're talking about.", message.User));
                        return;
                    }
                    LastHost.Type = pmatch.Groups["Type"].Value;
                    var Owner = pmatch.Groups["Owner"].Value;
                    switch (Owner)
                    {
                        case "my":
                            LastHost.Owner = instance.GetUser(message.User).Name;
                            break;
                        case "a":
                            if (string.IsNullOrEmpty(LastHost.Owner))
                            {
                                LastHost.Owner = "ADB";
                                //LastHost.IsFixture = true;
                            }
                            break;
                        case "the":
                            LastHost.Owner = "ADB";
                            LastHost.IsFixture = true;
                            break;
                        default:
                            LastHost.Owner = pmatch.Groups["un"].Value;
                            break;
                    }
                    instance.SendMessage(message.Channel, string.Format("Ok. {0} is {1}'s {2}.  I'll keep that in mind :simple_smile:", LastHost.FriendlyName, LastHost.Owner, LastHost.Type));
                    Singleton<NetworkData>.Instance.Save();
                }
                /* ****
                 * How long has it been here.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    @"How (long (has|is|was)? (it)? been t?here|old is it)",
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    instance.SendMessage(message.Channel, String.Format("{0} hours.", LastHost.Age));
                }

                /* ****
                 * Training Time.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    @"Training time",
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    var host = Singleton<NetworkData>.Instance.PickIncompleteHost();
                    (sender as Slack).SendMessage(message.Channel, String.Format("Do you recognise '{0}'?", host.FriendlyName));
                    LastHost = host;
                }

                /* ****
                 * Debug.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    string.Concat("@", instance.Self.Name, @":?\s+Debug ?(?<online>online)?"),
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
                {
                    bool online = !string.IsNullOrEmpty(pmatch.Groups["online"].Value);
                    int n = 0;
                    var sb = new StringBuilder();
                    foreach (var host in network.AllHosts)
                    {
                        if  (online && host.State == "down")
                            continue;
                        sb.AppendFormat("{0}: {1} ({2})", host.FriendlyName, network.Status(host), host.Age).AppendLine();
                        if (n++ == 10)
                        {
                            n = 0;
                            //Thread.Sleep(1000);
                        }
                    }
                    instance.SendMessage(message.Channel, sb.ToString());

                }
                /* ****
                 * Restart.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    string.Concat("@", instance.Self.Name, @":?\s+Re(start|boot)"),
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
                {
                    instance.SendMessage(message.Channel, "Rebooting!");
                    Running = false;
                }
                /* ****
                 * WOL.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    string.Concat("@", instance.Self.Name, @":?\s+Wake (?<name>\w+)"),
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
                {
                    LastHost = network.Find(pmatch.Groups["name"].Value);

                    instance.SendMessage(message.Channel, string.Format("Waking {0}!", LastHost.FriendlyName));
                    WOL.WakeOnLan(LastHost.HardwareAddress);
                    Running = false;
                }

                /* ****
                 * Ignore.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    @"Ignore (that|it)",
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    instance.SendMessage(message.Channel, string.Format("Setting {0} to a fixture.", LastHost.FriendlyName));
                    LastHost.IsFixture = true;
                    network.Save();
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

        public static Host LastHost { get; set; }
    }
}
