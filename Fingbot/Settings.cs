using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        [DataMember]
        public bool HasDoneIntroSpiel { get; set; }

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

        #region Indexer
        private Dictionary<string, PropertyInfo> Props = new Dictionary<string, PropertyInfo>();
        public object this[string pname]
        {
            get
            {
                var prop = GetProp(pname);
                return prop.GetValue(this, null);
            }
            set
            {
                var prop = GetProp(pname);
                prop.SetValue(this, value, null);
            }
        }

        private PropertyInfo GetProp(string pname)
        {
            if (Props==null)
                Props = new Dictionary<string, PropertyInfo>();
            if (!Props.ContainsKey(pname))
                Props[pname] = this.GetType().GetProperties().Single(n => n.Name == pname);
            return Props[pname];
        }
        #endregion
    }
}
