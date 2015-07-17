using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kamahl.Common;
using SlackRTM;
using SlackRTM.Events;

namespace Fingbot.Commands
{
    class ConfigCommand : ICommand
    {
        public bool Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance)
        {
            if (!IsTargeted)
                return false;
            var match = Regex.Match(
                    MessageText,
                    @".config (?<setting>[\w:]+)",
                    RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var settings = PersistentSingleton<Settings>.Instance;
                var value = settings[match.Groups["setting"].Value];
                RawMessage.Reply(Instance, string.Format("`.{0}` = `{1}`", match.Groups["setting"].Value, value), true);
                return true;
            }
            return false;
        }
    }
}
