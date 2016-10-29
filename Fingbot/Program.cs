using Fingbot.Commands;
using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Fingbot
{
    class Program
    {
        static Random random = new Random();

        static bool Running;
        static List<ICommand> Commands = new List<ICommand>() 
        { 
            new WhosInCommand(),
            new ThatsMyCommand(),
            new HowOldIsThatCommand(),
            new TrainingCommand(),
            new SelectCommand(),
            new IgnoreThatCommand(),
            new WakeOnCommand(),
            new DebugCommand(),
            new RemindMeToCommand(),
            new HelpCommand(),
            new NicknameCommand(),
            new ConfigCommand(),
        };

        static Dictionary<Slack, Thread> Idlefuncs = new Dictionary<Slack, Thread>();

        static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;
            string confdir;
            Directory.CreateDirectory(confdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fingbot"));
            Environment.CurrentDirectory = confdir;
            PersistentSingleton<Settings>.SavePath = "config.json";
#if REMINDERS
            PersistentSingleton<Reminders>.SavePath = "reminders.json";
#endif
            var settings = PersistentSingleton<Settings>.Instance;
            Singleton<NetworkData>.Instance.Refresh();
            //var missing = Singleton<NetworkData>.Instance.PickIncompleteHost();
            Singleton<NetworkData>.Instance.Save();

            var Slacks = settings.Tokens.Select(token => new Slack(token)).ToArray();
            if (Slacks.Length == 0)
            {
                Slack slack = new Slack();
                bool Authed = false;
                do
                {
                    if (String.IsNullOrEmpty(settings.Token))
                    {
                        Console.WriteLine("Please obtain a Token, and paste it below:");
                        settings.Token = Console.ReadLine();
                        PersistentSingleton<Settings>.Save();
                    }
                    slack = new Slack(settings.Token);
                    Authed = slack.Init();
                    if (!Authed)
                        settings.Token = "";

                } while (Authed == false);
                Slacks = new Slack[] { slack };
            }
            foreach (var slack in Slacks)
            {
                slack.OnEvent += slack_OnEvent;
                Console.WriteLine("Connecting...");
                slack.Connect();
                Running = true;

                //var idlefunc = new Thread(new ThreadStart(IdleFunc(slack)));
                //idlefunc.Start();

            }
            int attempts = 0;
            while (Running)
            {
                foreach (var slack in Slacks)
                {
                    if (!slack.Connected)
                    {
                        try
                        {
                            Console.WriteLine("Reconnecting...");
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
        }

        static void AppDomain_CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("Error.txt", e.ExceptionObject.ToString());
        }

        static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            File.WriteAllText("Error.txt", e.Exception.ToString());
        }

        private static Action IdleFunc(Slack slack)
        {
            return () =>
            {
                try
                {
                    Thread.CurrentThread.Name = slack.TeamInfo.Domain + "_idlefunc";
                    Thread.CurrentThread.IsBackground = true;
                    if (!PersistentSingleton<Settings>.Instance.HasDoneIntroSpiel)
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    DateTime LastQuestion = new DateTime();
                    int askchannel = random.Next(0, slack.JoinedChannels.Count());
                    while (true)
                    {
                        Singleton<NetworkData>.Instance.Refresh();
#if REMINDERS
                        try
                        {
                            PersistentSingleton<Reminders>.Instance.Check(slack);
                        }
                        catch (Exception)
                        { }
#endif
                        if (DateTime.Now.Hour < 7)
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
                }
                catch (Exception c)
                {
                    Console.WriteLine("!An Error has been caught!\n{0}", c);
                    File.WriteAllText("Idlefunc_error.txt", c.ToString());
                }
            };
        }

        static void slack_OnEvent(object sender, SlackEventArgs e)
        {
            try
            {
                var instance = sender as Slack;
                var network = Singleton<NetworkData>.Instance;
                if (!Idlefuncs.ContainsKey(instance) || !Idlefuncs[instance].IsAlive)
                {
                    Idlefuncs[instance] = new Thread(new ThreadStart(IdleFunc(instance)));
                    Idlefuncs[instance].Start();
                }
                //LogglyInst.Log(e.Data);
                if (e.Data.Type == "hello")
                {
                    Console.WriteLine(string.Format("Connected to {0}.", instance.TeamInfo.Name));
                    var settings = PersistentSingleton<Settings>.Instance;

                    if (!settings.HasDoneIntroSpiel)
                    {
                        var channel = instance.PrimaryChannel.IsMember ? instance.PrimaryChannel : instance.JoinedChannels.FirstOrDefault();
                        HelpCommand.Introduce(instance, channel);
                        PersistentSingleton<Settings>.Save();
                    }
                }
                if (e.Data is Message)
                {
                    network.Refresh();
                    var message = e.Data as Message;
                    if (message.Hidden)
                        return;
                    var substMessage = SubstituteMarkup(message.Text, sender as Slack);
                    Console.WriteLine(SubstituteMarkup(message.ToString(), sender as Slack));

                    if (message.User == instance.Self.Id)
                        return;

                    bool targeted = substMessage.StartsWith(string.Concat("@", instance.Self.Name), StringComparison.InvariantCultureIgnoreCase);
                    if (message.Channel[0] == 'D')
                        targeted = true;

                    foreach (var cmd in Commands)
                    {
                        bool fired = cmd.Run(substMessage, message, targeted, instance);
                        if (fired)
                            return;
                    }

                    // Leave this one here for now

                    /* ****
                     * Restart.
                     * ****/
                    var pmatch = Regex.Match(
                        SubstituteMarkup(message.Text, sender as Slack),
                        @"Re(start|boot)",
                        RegexOptions.IgnoreCase);
                    if (targeted && pmatch.Success)
                    {
                        instance.SendMessage(message.Channel, "Rebooting!");
                        Running = false;
                    }
                }
            }
            catch (Exception c)
            {
                Console.WriteLine("!An Error has been caught!\n{0}", c);
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
