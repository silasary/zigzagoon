using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SlackRTM;
using SlackRTM.Events;

namespace Fingbot.Commands
{
    class ConfigCommand : ICommand
    {
        public bool Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance)
        {
            var match = Regex.Match(
                    MessageText,
                    @".config (?<setting>[\w:]+)",
                    RegexOptions.IgnoreCase);
            
            return false;
        }
    }
}
