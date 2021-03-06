﻿using Kamahl.Common;
using SlackRTM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

#if REMINDERS
namespace Fingbot
{
    [DataContract]
    internal class Reminders
    {
        [DataMember]
        private List<Reminder> Notes = new List<Reminder>();

        internal void Add(User target, string p)
        {
            Notes.Add(new Reminder(target, p));
        }

        internal void Check(Slack slack)
       {
            try
            {
                var whosin = Singleton<NetworkData>.Instance.CertainHosts().Select(h => h.Owner).Distinct();
                foreach (var item in Notes.ToArray())
                {
                    var who = slack.GetUser(item.Who);
                    var name = "@" + who.Name;
                    if (whosin.Contains(name, System.StringComparer.CurrentCultureIgnoreCase))
                    {
                        var im = slack.Ims.FirstOrDefault(i => i.Name == who.Id);
                        if (im == null)
                        {
                            im = who.OpenIm();
                        }
                        slack.SendMessage(im.Id, "Reminder: " + item.Text);
                        Notes.Remove(item);
                    }
                }
                PersistentSingleton<Reminders>.Dirty();
            }
            catch (Exception c)
            {
                Console.WriteLine(c);
            }
        }

        private struct Reminder
        {
            public string Text;
            public string Who;
            public Reminder(User Who, string Text)
            {
                this.Who = Who.Id;
                this.Text = Text;
            }
        }
    }
}
#endif