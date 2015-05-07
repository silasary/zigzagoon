using SlackRTM;
using SlackRTM.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fingbot
{
    interface ICommand
    {
        bool Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance);
    }
}
