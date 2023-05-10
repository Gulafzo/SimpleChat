using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class IpAddressUtility
    {
        public static string GetLocalAddress()/// GetLocalAddress возвращает  IPадрес компьютера. Dns.GetHostEntry чтобы получить  DNS, потом извлекает IP-адресов из записи.
        {
            return Dns
                .GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(x => x.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        }

        public static IPAddress CreateBroadcastAddress()/// CreateBroadcastAddress создает IPадрес.  GetLocalAddress для получении IPдрес, затем изменяет последний байт на 255, для создании адрес широковещательной рассылки.созданный адресвозвращ.в виде объекта IPAddress.
        {
            string localIpAddess = GetLocalAddress();

            var localIpAddessNumbers = localIpAddess.Split('.');
            localIpAddessNumbers[3] = "255";
            var remoteIpAddressInString = localIpAddessNumbers
                .Aggregate("", (acc, value) => $"{acc}.{value}")
                .Substring(1);
            var broadcastAddress = IPAddress.Parse(remoteIpAddressInString);
            return broadcastAddress;
        }
    }
}
