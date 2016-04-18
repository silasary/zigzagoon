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
        private bool IsUp;

        public string State { get
            {
                if (IsUp == false)
                    return "down";
                if (DateTime.Now.Subtract(DateTime.FromFileTime(LastSeen)).TotalMinutes > 10)
                {
                    IsUp = false;
                    LastChange = DateTime.Now.ToString();
                }
                return "up";
            }
            set
            {
                var WasUp = IsUp;
                IsUp = value == "up";
                if (IsUp != WasUp)
                    LastChange = DateTime.Now.ToString();
                if (IsUp)
                    LastSeen = DateTime.Now.ToFileTime();
            }
        }
        
        [DataMember]
        public long LastSeen { get; set; }

        [DataMember]
        public string LastChange { get; set; }

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
                    return String.Format("`{0}` ({1})", HardwareAddress, Vendor);
                return HardwareAddress;
            }
        }

        public bool IsOld
        {
            get
            {
                if (LastChange == null)
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
                
                if (String.IsNullOrWhiteSpace(LastChange))
                    LastChange = DateTime.Now.ToString();
                try {
                    return DateTime.Now.Subtract(DateTime.Parse(this.LastChange)).TotalHours;
                }
                catch (Exception c)
                {
                    LastChange = DateTime.Now.ToString();
                    return 0;
                }
            }
            set { }
        }

        public bool Uncertain { get; set; }
    }
}
