using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                LastHost.Name = match.Groups["Nickname"].Value;
                
            }
            return false;
        }
    }
}
