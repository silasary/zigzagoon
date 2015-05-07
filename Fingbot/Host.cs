using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fingbot
{
    [DataContract(Namespace = "")]
    class Host
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Hostname { get; set; }

        [DataMember]
        public string HardwareAddress { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public string Vendor { get; set; }

        [DataMember]
        public string State { get; set; }

        [DataMember]
        public string RawState { get; set; }

        [DataMember]
        public string LastChangeTime { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Owner { get; set; }

        [DataMember]
        public bool IsFixture { get; set; }

        public string FriendlyName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                    return Name;
                if (!string.IsNullOrWhiteSpace(Hostname))
                    return Hostname;
                if (!string.IsNullOrEmpty(Vendor))
                    return String.Format("{0} ({1})", HardwareAddress, Vendor);
                return HardwareAddress;
            }
        }

        public bool IsOld
        {
            get
            {
                if (LastChangeTime == null)
                    //LastChangeTime = this.FirstSeen;
                    return false;
                return Age > PersistentSingleton<Settings>.Instance.MaxAge;
            }
        }
        [DataMember]
        public double Age
        {
            get
            {
                if (String.IsNullOrWhiteSpace(LastChangeTime))
                    LastChangeTime = DateTime.Now.ToString();
                return DateTime.Now.Subtract(DateTime.Parse(this.LastChangeTime)).TotalHours;
            }
            set { }
        }

        public bool Uncertain { get; set; }
    }
}
