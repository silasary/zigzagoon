using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class SelectCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
             * Select.
             * ****/
            var match = Regex.Match(
                MessageText,
                @"(Select|Pick|With) (?<name>[\w:]+)",
                RegexOptions.IgnoreCase);
            if (IsTargeted && match.Success)
            {
                Host LastHost;
                NetworkData network = Singleton<NetworkData>.Instance;
                Instance.SetLastHost(LastHost = network.Find(match.Groups["name"].Value));
                if (LastHost == null)
                {
                    Instance.SendMessage(RawMessage.Channel, "I couldn't find it");
                    return true;
                }
                Instance.SendMessage(RawMessage.Channel, string.Format("{0}: {1} ({2})", LastHost.FriendlyName, network.Status(LastHost), LastHost.Age));
                return true;
            }
            return false;
        }
    }
}
