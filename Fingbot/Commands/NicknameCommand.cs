using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kamahl.Common;

namespace Fingbot.Commands
{
    class NicknameCommand : ICommand
    {
        public bool Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance)
        {
            var match = Regex.Match(
                MessageText,
                @"(Call|Name|Nickname) it (?<Nickname>[\w ]+)?",
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (!IsTargeted)
                {
                    //TODO: Rate Limit to once per day.
                    RawMessage.Reply(Instance, "Was that to me?");
                    return true;
                }
                var LastHost = Instance.GetLastHost();
                if (LastHost == null)
                {
                    RawMessage.Reply(Instance, "I have no idea what you're talking about.", true);
                    return true;
                }
                var oldname = LastHost.FriendlyName;
                LastHost.Name = match.Groups["Nickname"].Value;
                RawMessage.Reply(Instance, string.Format("Ok. It'll call {0} \"{1}\" from now on.", oldname, LastHost.FriendlyName));
                return true;
            }
            match = Regex.Match(
                    MessageText,
                    @"(Call|Name|Nickname) (?<name>[\w:]+) (as|to) (?<Nickname>[\w ]+)?",
                    RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (!IsTargeted)
                {
                    //TODO: Rate Limit to once per day.
                    RawMessage.Reply(Instance, "Was that to me?");
                    return true;
                }
                Host LastHost;
                NetworkData network = Singleton<NetworkData>.Instance;
                Instance.SetLastHost(LastHost = network.Find(match.Groups["name"].Value));
                if (LastHost == null)
                {
                    Instance.SendMessage(RawMessage.Channel, "I couldn't find anything by that name");
                    return true;
                }
                var oldname = LastHost.FriendlyName;
                LastHost.Name = match.Groups["Nickname"].Value;
                RawMessage.Reply(Instance, string.Format("Ok. It'll call {0} \"{1}\" from now on.", oldname, LastHost.FriendlyName));
                return true;
            }
            return false;
        }
    }
}
