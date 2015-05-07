﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fingbot
{
    [DataContract]
    class Settings
    {
        [DataMember(Name="Token", IsRequired=false, EmitDefaultValue=false)]
        private string token;

        public string Token { 
            get { return string.Join(";", Tokens); } 
            set { Tokens.Clear(); Tokens.AddRange(value.Split(';')); } 
        }
        [DataMember]
        public List<String> Tokens { get; set; }
        [DataMember]
        public string FingXml { get; set; }

        [DataMember]
        public string FingArgs { get; set; }

        [DataMember]
        public double MaxAge { get; set; }

        [DataMember]
        private int SettingsVersion;

        [OnDeserialized]
        private void Setup(StreamingContext e)
        {
            if (MaxAge == 0.0)
                MaxAge = 12;
            if (string.IsNullOrEmpty(FingArgs))
                FingArgs = "--session data.dat -o table,xml,fing.xml -o table,csv,console";
            if (string.IsNullOrEmpty(FingXml))
                FingXml = "fing.xml";
            if (SettingsVersion < 1)
            {
                Tokens = new List<string>();
                if (token !=null)
                    Tokens.AddRange(token.Split(';'));
                token = null;
                SettingsVersion = 1;

            }
        }
        public Settings()
        {
            Setup(new StreamingContext(StreamingContextStates.Other));
        }
    }
}
