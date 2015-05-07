using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class HowOldIsThatCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
 * How long has it been here.
 * ****/
            var match = Regex.Match(
                MessageText,
                @"How (long (has|is|was)? (it)? been t?here|old is it)",
                RegexOptions.IgnoreCase);
            if (IsTargeted && match.Success)
            {
                var LastHost = Instance.GetLastHost();
                if (LastHost == null)
                {
                    Instance.SendMessage(RawMessage.Channel, String.Format("@{0}: How long has _what_ been been here?", Instance.GetUser(RawMessage.User).Name));
                    return true;
                }
                Instance.SendMessage(RawMessage.Channel, String.Format("{0} hours.", LastHost.Age));
                return true;
            }
            return false;
        }
    }
}
