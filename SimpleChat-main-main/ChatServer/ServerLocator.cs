using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ChatServer
{
    internal class ServerLocator : IDisposable //это класс находить серверы и работает на двух потоках: ServerLocatorSender и ServerLocatorReciever.
    {
        public static int Port = 0;
        private bool _isStarted;
        private readonly Thread _serverLocatorSenderThread;
        private readonly Thread _serverLocatorResieverThread;
        private readonly Socket _udpBroadcastSocketReciever;
        private readonly List<string> _listRequests;
        private static int _portReciever;
        private static object _lockListRequests;

        public ServerLocator()
        {
            _isStarted = false;
            _portReciever = 11111;
            _serverLocatorSenderThread = new Thread(ServerLocatorSender);
            _serverLocatorResieverThread = new Thread(ServerLocatorReciever);

            _udpBroadcastSocketReciever = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _udpBroadcastSocketReciever.EnableBroadcast = true;

            _listRequests = new List<String>();
            _lockListRequests = new object();
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

        private void ServerLocatorSender()//метод ServerLocatorSender отправляет запросы на поиск серверов и проверяет номер порта, отправляет запрос на все IP-адреса и порты, которые были добавлены в список запросов потом удаляет  адрес который уже был отправлен
        {
            while (_isStarted)
            {
                if (Port == 0)
                    continue;
                string IP_Adress_Port = "";
                lock (_lockListRequests)
                {
                    if (_listRequests.Count > 0)
                    {
                        IP_Adress_Port = _listRequests[0];
                    }
                }
                if (IP_Adress_Port == "")
                {
                    Task.Delay(100).Wait();
                    continue;
                }
                var mass = IP_Adress_Port.Split(':');
                if (mass.Length == 0)
                {
                    lock (_lockListRequests)
                    {
                        IP_Adress_Port.Remove(0);
                    }
                    continue;
                }
                Socket udpBroadcastSocketSender = new Socket(SocketType.Dgram, ProtocolType.Udp);
                IPAddress broadcastAddress = IpAddressUtility.CreateBroadcastAddress();
                int port = int.Parse(mass[1]);
                var broadcastIpEndPoint = new IPEndPoint(IPAddress.Parse(mass[0]), port);
                udpBroadcastSocketSender.Connect(broadcastIpEndPoint);
                string Message = IpAddressUtility.GetLocalAddress() + ":" + Port;
                Console.WriteLine("ServerLocatorSender");
                Console.WriteLine(Message);
                SocketUtility.SendString(udpBroadcastSocketSender, Message, () => { });
                lock (_lockListRequests)
                {
                    _listRequests.RemoveAt(0);
                }
                Task.Delay(100).Wait();
                udpBroadcastSocketSender.Close();
            }
        }

        private void ServerLocatorReciever()//Метод ServerLocatorReciever принимает запросы на поиск серверов, принимает все входящие сообщения и добавляет сообщения в список запросов
        {
            try
            {
                _udpBroadcastSocketReciever.Bind(new IPEndPoint(IPAddress.Any, _portReciever));
                Console.WriteLine("Reciever port - " + _portReciever);
            }
            catch
            {
                _portReciever++;
                ServerLocatorReciever();
                return;
            }

            while (_isStarted)
            {
                string stroka = SocketUtility.ReceiveString(_udpBroadcastSocketReciever);
                Console.WriteLine("ServerLocatorReciever");
                Console.WriteLine(stroka);
                lock (_lockListRequests)
                {
                    _listRequests.Add(stroka);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _udpBroadcastSocketReciever.Dispose();
        }
    }
}
