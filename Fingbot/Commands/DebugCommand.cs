﻿using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class DebugCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
             * Debug.
             * ****/
            var match = Regex.Match(
                MessageText,
                
                string.Concat("Debug ?((?<online>online)?|(?<mine>mine)|(?<unknown>unknown))"),
                RegexOptions.IgnoreCase);
            if (!IsTargeted)
                return false; // No inform here.
            if (match.Success)
            {
                bool online = !string.IsNullOrEmpty(match.Groups["online"].Value);
                bool mine = !string.IsNullOrEmpty(match.Groups["mine"].Value);
                bool unknown = !string.IsNullOrEmpty(match.Groups["online"].Value);
                int n = 0;
                var sb = new StringBuilder();
                NetworkData network = Singleton<NetworkData>.Instance;
                foreach (var host in network.AllHosts)
                {
                    if (online && host.State == "down")
                        continue;
                    if (mine && host.Owner != RawMessage.User) // TODO: Utterly broken - Not the same.
                        continue;
                    if (unknown && !string.IsNullOrEmpty(host.Owner))
                        continue;
                    sb.AppendFormat("{0}: {1} ({2})", host.FriendlyName, network.Status(host), host.Age).AppendLine();
                    if (n++ == 10)
                    {
                        n = 0;
                    }
                }
                Instance.SendMessage(RawMessage.Channel, sb.ToString());
                return true;
            }
            return false;
        }
    }
}
