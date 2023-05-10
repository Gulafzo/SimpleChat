using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ChatClient
{
    internal class ServerLocator : IDisposable// ServerLocator- локатор сервера, находить серверы в сети и  работает на двух потоках: 1 ServerLocatorSender 2 ServerLocatorReciever.
    {
        private static List<IPEndPoint> _servers;
        private static object _lockServers;
        private bool _isStarted;
        private readonly Thread _serverLocatorSenderThread;
        private readonly Thread _serverLocatorResieverThread;
        private readonly Socket _udpBroadcastSocketSender;
        private readonly Socket _udpBroadcastSocketResiever;
        private static int _portReciever;

        public List<IPEndPoint> Servers => _servers;

        public ServerLocator()
        {
            _servers = new List<IPEndPoint>();
            _lockServers = new object();
            _isStarted = false;

            _serverLocatorSenderThread = new Thread(ServerLocatorSender);
            _serverLocatorResieverThread = new Thread(ServerLocatorReciever);

            _udpBroadcastSocketSender = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _udpBroadcastSocketSender.EnableBroadcast = true;
            _udpBroadcastSocketResiever = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _portReciever = CreatePort();
        }

        public void Start()
        {
            _isStarted = true;
            _serverLocatorSenderThread.Start();
            _serverLocatorResieverThread.Start();
        }

        public void Stop()
        {
            _isStarted = false;

            Task.Delay(100).Wait();

            _serverLocatorResieverThread.Abort();
            _serverLocatorSenderThread.Abort();
        }

        private void ServerLocatorSender()// ServerLocatorSender отправляет широковещательные сообщения на порт 11111,передается IPадрес и порт. CreatePort это метод генерирует случаный порт от 11000 до 11009.
        {
            IPAddress broadcastAddress = IpAddressUtility.CreateBroadcastAddress();
            var broadcastIpEndPoint = new IPEndPoint(broadcastAddress, 11111);
            _udpBroadcastSocketSender.Connect(broadcastIpEndPoint);
            string Message = IpAddressUtility.GetLocalAddress() + ":" + _portReciever;

            while (_isStarted)
            {
                Console.WriteLine("ServerLocatorSender - " + 11111);
                Console.WriteLine(Message);
                Console.WriteLine("");
                SocketUtility.SendString(_udpBroadcastSocketSender, Message, () => { });
                Task.Delay(10).Wait();
            }
        }

        private static int CreatePort()//CreatePort это метод генерирует случаный порт от 11000 до 11009
        {
            Random rnd = new Random();
            int value = rnd.Next(0, 10);
            value += 11000;
            return value;
        }

        private void ServerLocatorReciever()// ServerLocatorReciever принимает широковещательные сообщения на порту _portReciever передается IPадрес и порт  
        {
            _udpBroadcastSocketResiever.Bind(new IPEndPoint(IPAddress.Any, _portReciever));

            while (_isStarted)
            {
                string stroka = SocketUtility.ReceiveString(_udpBroadcastSocketResiever);
                Console.WriteLine("ServerLocatorReciever");
                Console.WriteLine(stroka);
                Console.WriteLine("");
                var mass = stroka.Split(':');
                if (mass.Length == 0)
                    continue;
                IPEndPoint iP = new IPEndPoint(IPAddress.Parse(mass[0]), int.Parse(mass[1]));
                lock (_lockServers)
                {
                    if (!_servers.Contains(iP))
                    {
                        _servers.Add(iP);
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _udpBroadcastSocketSender.Dispose();
        }
    }
}
