using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fingbot.Commands
{
    class ThatsMyCommand : ICommand
    {
        bool ICommand.Run(string MessageText, SlackRTM.Events.Message RawMessage, bool IsTargeted, SlackRTM.Slack Instance)
        {
           /* ****
            * That's my Something
            * ****/
            var match = Regex.Match(
                MessageText,
                @"That['’]?s (?<Owner>a|my|the|(?<un>@[\w]+)'s) (?<Type>[\w ]+)?",
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
                    Instance.SendMessage(RawMessage.Channel, String.Format("@{0}: I have no idea what you're talking about.", Instance.GetUser(RawMessage.User).Name));
                    return true;
                }
                LastHost.Type = match.Groups["Type"].Value;
                var Owner = match.Groups["Owner"].Value;
                switch (Owner)
                {
                    case "my":
                        LastHost.Owner = Instance.GetUser(RawMessage.User).Name;
                        break;
                    case "a":
                        if (string.IsNullOrEmpty(LastHost.Owner))
                        {
                            LastHost.Owner = "";
                            //LastHost.IsFixture = true;
                        }
                        break;
                    case "the":
                        LastHost.Owner = "?";
                        LastHost.IsFixture = true;
                        break;
                    default:
                        LastHost.Owner = match.Groups["un"].Value;
                        break;
                }
                Instance.SendMessage(RawMessage.Channel, string.Format("Ok. {0} is {1}'s {2}.  I'll keep that in mind :simple_smile:", LastHost.FriendlyName, LastHost.Owner, LastHost.Type));
                Singleton<NetworkData>.Instance.Save();
                return true;
            }
        return false;
        }
    }
}
