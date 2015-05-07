using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fingbot.Commands
{
    class HelpCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            return false;
        }
    }
}
