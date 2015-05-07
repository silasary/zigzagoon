using SlackRTM;
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
    }
}
