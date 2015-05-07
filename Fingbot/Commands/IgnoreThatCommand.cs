using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class IgnoreThatCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
                 * Ignore.
                 * ****/
            var match = Regex.Match(
                MessageText,
                @"Ignore (that|it)",
                RegexOptions.IgnoreCase);
            if (IsTargeted && match.Success)
            {
                var LastHost = Instance.GetLastHost();
                if (LastHost == null)
                {
                    Instance.SendMessage(RawMessage.Channel, String.Format("@{0}: I don't know what you want me to ignore.", Instance.GetUser(RawMessage.User).Name));
                    return true;
                }
                Instance.SendMessage(RawMessage.Channel, string.Format("Ok. I'll ignore {0} from now on.", LastHost.FriendlyName));
                LastHost.IsFixture = true;
                Singleton<NetworkData>.Instance.Save();
                return true;
            }
            return false;
        }
    }
}
