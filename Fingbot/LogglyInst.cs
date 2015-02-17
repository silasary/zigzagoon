using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Fingbot
{
    class LogglyInst
    {
        static WebClient wc = new WebClient();
        public static void Log(SlackRTM.Events.Event e)
        {
            wc.UploadString(new Uri("http://logs-01.loggly.com/inputs/88bf8f14-e94f-49f7-bb04-f55d5a52dc75/tag/http/"), e.ToJson());   

        }
    }
}
