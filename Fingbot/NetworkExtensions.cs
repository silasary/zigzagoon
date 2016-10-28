using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Fingbot
{
    static class NetworkExtensions
    {
        public static IPAddress CalculateNetwork(this UnicastIPAddressInformation addr)
        {
            // The mask will be null in some scenarios, like a dhcp address 169.254.x.x
            if (addr.IPv4Mask == null)
                return null;

            var ip = addr.Address.GetAddressBytes();
            var mask = new byte[4];
            try
            {
                mask = addr.IPv4Mask.GetAddressBytes();
            }
            catch (System.Reflection.TargetInvocationException) // Mono
            {
                mask = new byte[] { 255, 255, 255, 0 };
            }

            var result = new Byte[4];
            for (int i = 0; i < 4; ++i)
            {
                result[i] = (Byte)(ip[i] & mask[i]);
            }

            return new IPAddress(result);
        }
    }
}
