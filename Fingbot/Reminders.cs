using Kamahl.Common;
using SlackRTM;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

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
            var whosin = Singleton<NetworkData>.Instance.CertainHosts().Select(h => h.Owner).Distinct();
            foreach (var item in Notes.ToArray())
            {
                var who = slack.GetUser(item.Who);
                var name = "@" + who.Name;
                if (whosin.Contains(name, System.StringComparer.CurrentCultureIgnoreCase))
                {
                    
                    slack.SendMessage(name, "Reminder: " + item.Text);
                    Notes.Remove(item);
                }
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