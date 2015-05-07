using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class WakeOnCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
             * WOL.
             * ****/
            var match = Regex.Match(
                MessageText,
                // TODO: Use IsTargeted
                string.Concat("@", Instance.Self.Name, @":?\s+Wake (?<name>\w+)"),
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                NetworkData network = Singleton<NetworkData>.Instance;
                var LastHost = network.Find(match.Groups["name"].Value);
                Instance.SetLastHost(LastHost);
                Instance.SendMessage(RawMessage.Channel, string.Format("Waking {0}!", LastHost.FriendlyName));
                WOL.WakeOnLan(LastHost.HardwareAddress);
                return true;
            }
            return false;
        }
    }
}
