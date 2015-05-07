using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class RemindMeToCommand : ICommand
    {
        public bool Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
             * Reminders.
             * ****/
            var match = Regex.Match(
                MessageText,
                @"Remind (?<Owner>me|(?<un>@[\w]+)) to (?<Text>.+)",
                RegexOptions.IgnoreCase);
            if (IsTargeted && match.Success)
            {
                var target = Instance.GetUser(match.Groups["un"].Success ? match.Groups["un"].Value : RawMessage.User);
                Instance.SendMessage(RawMessage.Channel, string.Format("Ok. I'll remind {0} next time {1} in.", target.Name, "they're"));
                PersistentSingleton<Reminders>.Instance.Add(target, match.Groups["Text"].Value);
                PersistentSingleton<Reminders>.Instance.Check(Instance);
                return true;
            }
            return false;
        }
    }
}
