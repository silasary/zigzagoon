using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fingbot
{
    internal static class SlackExtensions
    {
        private static Dictionary<Slack, Host> LastHosts = new Dictionary<Slack,Host>();
        public static void SetLastHost(this Slack instance, Host host)
        {
            LastHosts[instance] = host;
        }
        public static Host GetLastHost(this Slack instance)
        {
            Host value;
            LastHosts.TryGetValue(instance, out value);
            return value;
        }

        public static void Reply(this Message message, Slack Instance, string Text, bool targeted = false)
        {
            if (targeted)
            {
                Text = string.Format("{1}: {0}", Text, Instance.GetUser(message.User).Name);
            }
            Instance.SendMessage(message.Channel, Text);
        }
    }
}
