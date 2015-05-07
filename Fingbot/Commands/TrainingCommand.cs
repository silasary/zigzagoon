using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class TrainingCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
            /* ****
                 * Training Time.
                 * ****/
            var match = Regex.Match(
                MessageText,
                @"Training time",
                RegexOptions.IgnoreCase);
            if (IsTargeted && match.Success)
            {
                var host = Singleton<NetworkData>.Instance.PickIncompleteHost();
                if (host == null)
                    Instance.SendMessage(RawMessage.Channel, "I know everything here.");
                else
                    Instance.SendMessage(RawMessage.Channel, String.Format("Do you recognise '{0}'?", host.FriendlyName));
                Instance.SetLastHost(host);
                return true;
            }
            return false;
        }
    }
}
