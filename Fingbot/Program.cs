using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            if (e.Data.Type == "hello")
            {
                Console.WriteLine("Connected.");
            }
            if (e.Data is Message)
            {
                Singleton<NetworkData>.Instance.Refresh();
                var message = e.Data as Message;
                if (message.Hidden)
                    return;
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
                    LastHost = host;
                    return;
                }
                /* ****
                 * That's my Something
                 * ****/
                var pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    string.Concat("@", instance.Self.Name, @":?\s+That's (?<Owner>a|my|the|(?<un>@[\w]+)'s) (?<Type>\w+)?"), 
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
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
                    string.Concat("@", instance.Self.Name, @":?\s+How long (has|is|was)? (it)? been t?here"),
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
                {
                    instance.SendMessage(message.Channel, String.Format("{0} hours.", LastHost.Age));
                }

                /* ****
                 * Training Time.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    string.Concat("@", instance.Self.Name, @":?\s+Training time"),
                    RegexOptions.IgnoreCase);
                if (pmatch.Success)
                {
                    var host = Singleton<NetworkData>.Instance.PickIncompleteHost();
                    (sender as Slack).SendMessage(message.Channel, String.Format("Do you recognise '{0}'?", host.FriendlyName));
                    LastHost = host;
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
