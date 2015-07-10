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
    class HelpCommand : ICommand
    {
        bool ICommand.Run(string MessageText, Message RawMessage, bool IsTargeted, Slack Instance)
        {
            var match = Regex.Match(
               MessageText,
               @"Introduce yourself",
               RegexOptions.IgnoreCase);
            if (match.Success && IsTargeted)
                Introduce(Instance, Instance.GetChannel(RawMessage.Channel));
            return false;
        }

        public static void Introduce(Slack instance, Channel channel)
        {
            var settings = PersistentSingleton<Settings>.Instance;

            instance.SendMessage(channel, "Hi, I'm {0}!", instance.Self.Name);
            instance.SendMessage(channel,
                "I'm here to keep an eye on your building while you're not around.\n" +
                "I do this by watching your wifi network. " +
                "I'll keep notes on each device that connects to the wifi, and with your help, be able to work out who's in the building.\n");
            instance.SendMessage(channel, "If you ever need me, just ask _Who's in?_ or _Anyone in?_, and I'll let you know.\n" +
                "If there's a device I don't recognise, just say _@{0}: That's my iThing_, or _@{0}: That's @somebody's bright pink phone_.\n" +
                "I try to work out what belongs here, and try not to ask about the router or printer, but if I ever do, feel free to let me know by saying _@{0}: Ignore that_.", instance.Self.Name);
            settings.HasDoneIntroSpiel = true;
        }
    }
}
