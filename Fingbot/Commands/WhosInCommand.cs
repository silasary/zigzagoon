using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class WhosInCommand : ICommand
    {

        bool ICommand.Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance)
        {
            // ’
            var match = Regex.Match(
               MessageText,
               @"(Who['’]?s|(Any|Some)(one|body)) in\?",
               RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var hosts = Singleton<NetworkData>.Instance.CertainHosts().ToList();
                var unknowns = Singleton<NetworkData>.Instance.UnknownHosts().Count();
                var people = new List<string>();
                Host h = null;
                foreach (var host in hosts.ToArray())
                {
                    if (people.Contains(host.Owner))
                        hosts.Remove(host);
                    else
                        people.Add(host.Owner);
                }

                if (hosts.Count > 0)
                    Instance.SendMessage(RawMessage.Channel, string.Format("{0} {1} here. {2}",
                        string.Join(", ", hosts.Select(host => String.Format("{0}'s {1} '{2}'", host.Owner, host.Type, host.FriendlyName))),
                        hosts.Count == 1 ? "is" : "are",
                        unknowns > 0 ? string.Format("There are also {0} unknown devices.", unknowns) : ""));
                else if ((h = Singleton<NetworkData>.Instance.PickIncompleteHost()) != null)
                {
                    Instance.SendMessage(RawMessage.Channel, String.Format("I don't know. But there is a device I don't recognise: {0}", h.FriendlyName));
                }
                else
                    Instance.SendMessage(RawMessage.Channel, "Nobody. It's lonely here :frowning:");
                Instance.SetLastHost(h);
                return true;
            }
            return false;
        }
    }
}
