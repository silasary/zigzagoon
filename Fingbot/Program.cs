using Fingbot.Commands;
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
using System.Threading.Tasks;

namespace Fingbot
{
    class Program
    {
        static bool Running;
        static List<ICommand> Commands = new List<ICommand>() 
        { 
            new WhosInCommand(),
            new ThatsMyCommand(),
            new HowOldIsThatCommand(),
            new TrainingCommand(),
            new SelectCommand(),
            new IgnoreThatCommand(),
            new WakeOnCommand()
        };
        static void Main(string[] args)
        {
            string confdir;
            Directory.CreateDirectory(confdir= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fingbot"));
            Environment.CurrentDirectory = confdir;
            PersistentSingleton<Settings>.SavePath = "config.json";
            PersistentSingleton<Reminders>.SavePath = "reminders.json";
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

            Task.Factory.StartNew(IdleFunc(slack));

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

        private static Action IdleFunc(Slack slack)
        {
            return () =>
            {
                DateTime LastQuestion = new DateTime();
                int askchannel =0;
                while (true)
                {
                    Singleton<NetworkData>.Instance.Refresh();
                    try
                    {
                        PersistentSingleton<Reminders>.Instance.Check(slack);
                    }
                    catch (Exception)
                    { }
                    if (DateTime.Now.Hour < 10)
                        continue;
                    var inc = Singleton<NetworkData>.Instance.PickIncompleteHost();
                    if (inc != null && LastQuestion.Date != DateTime.Now.Date)
                    {
                        LastQuestion = DateTime.Now;
                        string chan = slack.JoinedChannels.ElementAt(askchannel).Id;
                        slack.SendMessage(chan, "Excuse me, but does anyone recognise '{0}'?", inc.FriendlyName);
                        askchannel++;
                        if (askchannel == slack.JoinedChannels.Count())
                            askchannel = 0;
                        slack.SetLastHost(inc);
                    }
                    Thread.Sleep(new TimeSpan(0, 5, 0));
                }
            };
        }

        static void slack_OnEvent(object sender, SlackEventArgs e)
        {
            var instance = sender as Slack;
            var network = Singleton<NetworkData>.Instance;
            LogglyInst.Log(e.Data);
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
                var substMessage = SubstituteMarkup(message.ToString(), sender as Slack);
                Console.WriteLine(substMessage);
                
                if (message.User == instance.Self.Id)
                    return;

                bool targeted = SubstituteMarkup(message.Text, sender as Slack).StartsWith(string.Concat("@", instance.Self.Name), StringComparison.InvariantCultureIgnoreCase);
                if (message.Channel[0] == 'D')
                    targeted = true;

                foreach (var cmd in Commands)
                {
                    bool fired = cmd.Run(substMessage, message, targeted, instance);
                    if (fired)
                        return;
                }

                Match pmatch;

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
                    @"Re(start|boot)",
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    instance.SendMessage(message.Channel, "Rebooting!");
                    Running = false;
                }
                


                /* ****
                 * Reminders.
                 * ****/
                pmatch = Regex.Match(
                    SubstituteMarkup(message.Text, sender as Slack),
                    @"Remind (?<Owner>me|(?<un>@[\w]+)) to (?<Text>.+)", 
                    RegexOptions.IgnoreCase);
                if (targeted && pmatch.Success)
                {
                    var target = instance.GetUser(pmatch.Groups["un"].Success ? pmatch.Groups["un"].Value : message.User);
                    instance.SendMessage(message.Channel, string.Format("Ok. I'll remind {0} next time {1} in.", target.Name, "they're"));
                    PersistentSingleton<Reminders>.Instance.Add(target, pmatch.Groups["Text"].Value);
                    PersistentSingleton<Reminders>.Instance.Check(instance);
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
