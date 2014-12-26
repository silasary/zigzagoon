using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fingbot
{
    [DataContract(Namespace="")]
    class Host
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Hostname { get; set; }

        [DataMember]
        public string HardwareAddress{ get; set; }

        [DataMember]
        public string Address{ get; set; }

        [DataMember]
        public string Vendor{ get; set; }

        [DataMember]
        public string State{ get; set; }

        [DataMember]
        public string LastChangeTime { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Owner { get; set; }

        [DataMember]
        public bool IsFixture { get; set; }

        public object FriendlyName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                    return Name;
                if (!string.IsNullOrWhiteSpace(Hostname))
                    return Hostname;
                return HardwareAddress;
            }
        }
    }
}
