using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fingbot
{
    [DataContract]
    class Settings
    {
        [DataMember]
        public string Token { get; set; }

        [DataMember]
        public string Channel { get; set; }

        [DataMember]
        public string FingXml { get; set; }

        [DataMember]
        public string FingArgs { get; set; }

        [DataMember]
        public double MaxAge { get; set; }

        [OnDeserialized]
        private void Setup(StreamingContext e)
        {
            if (MaxAge == 0.0)
                MaxAge = 12;
            if (string.IsNullOrEmpty(FingArgs))
                FingArgs = "--session data.dat -o table,xml,fing.xml -o table,csv,console";
            if (string.IsNullOrEmpty(FingXml))
                FingXml = "fing.xml";
        }
        public Settings()
        {
            Setup(new StreamingContext(StreamingContextStates.Other));
        }
    }
}
