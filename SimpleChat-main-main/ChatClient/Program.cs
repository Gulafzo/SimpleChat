using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace ChatClient
{
    internal class Program
    {
        private static bool _isClientAlive = false;
        private static bool _StopWaitingData = false;
        private static bool _StopSearchingServers = false;
        static void Main(string[] args)
        {
            using (var serverLocator = new ServerLocator())
            {
                serverLocator.Start();

                var socket = ServerSelection(serverLocator);

                var Nickname = GetClientName();
                SendMessageToServer(socket, Nickname);
                do
                {
                    Information();
                    WaitingData(socket);
                    var message = GetClientMessage();
                    SendMessageToServer(socket, message);
                    if (message == "")
                        ExitTheChat(socket);

                } while (!_isClientAlive);
            }
        }

        private static void Information()
        {
            Console.WriteLine("To finish waiting for a message to be received, press Enter.");
            Console.WriteLine("To exit, send an empty message.");
        }

        private static Socket ServerSelection(ServerLocator locator)
        {
            StopServerSearch(locator);
            while (true)
            {
                if (_StopSearchingServers)
                {
                    int numberServer;
                    var servers = locator.Servers;
                    Console.WriteLine("Выбетие сервер: ");
                    for (int i = 0; i < servers.Count; i++)
                    {
                        Console.WriteLine($"{i} {servers[i]}");
                    }
                    Console.Write("Введите номер сервера к которому хотите подключиться - ");
                    try
                    {
                        numberServer = int.Parse(Console.ReadLine());
                        if (!(numberServer >= 0 && servers.Count > numberServer))
                            throw new Exception();
                    }
                    catch
                    {
                        Console.WriteLine("Введен не правельный номер серрвера");
                        continue;
                    }
                    //var mass = servers[numberServer].Split(':');
                    //return ConnectClientToServer(new IPEndPoint(IPAddress.Parse(mass[0]), int.Parse(mass[1])));
                    return ConnectClientToServer(servers[numberServer]);
                }
                else
                {
                    Task.Delay(1000);
                    continue;
                }
            }
        }

        private static async void StopServerSearch(ServerLocator locator)
        {
            await Task.Run(() => Task.Delay(3000));
                locator.Stop();
                _StopSearchingServers = true;
        }


        private static void WaitingData(Socket socket)
        {
            _StopWaitingData = false;
            ByPressingTheEndDataWaitingKey();
            Console.WriteLine("Waiting for messages");
            bool OutputНeaders = true;
            while (!_StopWaitingData)
            {
                while (socket.Available < 1 && !_StopWaitingData)
                {
                    Thread.Sleep(100);
                }
                if (!_StopWaitingData)
                {
                    if (OutputНeaders)
                    {
                        OutputНeaders = false;
                        Console.WriteLine("---------------Chat content--------------------");
                    }
                    var chatContent = ReceiveChatContent(socket);
                    Console.WriteLine(chatContent);
                }
            }
            if (!OutputНeaders)
            {
                Console.WriteLine("------------End of chat content----------------");
                Console.WriteLine();
            }
        }

        // при нажатии Enter прекращает ожидать данные из сети
        private static async void ByPressingTheEndDataWaitingKey()
        {
            await Task.Run(() => WaitingKeyPress(ConsoleKey.Enter));
            _StopWaitingData = true;
        }
        // Ожидает нажатие определенной клавиши. Клавиша которую нужно
        // ожидать нажатие передается как входной параметр
        private static void WaitingKeyPress(ConsoleKey key)
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey();
            } while (keyInfo.Key != key);
        }

        private static void ExitTheChat(Socket socket)
        {
            _isClientAlive = true;

            DisconnectClientFromServer(socket);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            DisposeClientSocket(socket);
            Environment.Exit(0);
        }

        private static void DisposeClientSocket(Socket socket)
        {
            socket.Close();
            socket.Dispose();
        }

        private static void DisconnectClientFromServer(Socket socket)
        {
            socket.Disconnect(false);
            Console.WriteLine("Client disconnected from server");
        }

        private static void SendMessageToServer(Socket socket, string message)
        {
            if (message == "")
                SocketUtility.SendString(socket, message,
                    () => { Console.WriteLine($"Send string to server data check client side exception"); });
            else
            {
                Console.WriteLine("Sending message to server");
                SocketUtility.SendString(socket, message,
                    () => { Console.WriteLine($"Send string to server data check client side exception"); });
                Console.WriteLine("Message sent to server");
            }
        }

        private static string GetClientMessage()
        {
            Console.WriteLine("");
            Console.Write("Your message:");
            var message = Console.ReadLine();
            return message;
        }

        private static string GetClientName()
        {
            string message;
            do
            {
                Console.Write("Your name:");
                message = Console.ReadLine();
                if (message == "")
                {
                    Console.WriteLine("You didn't enter a name");
                }
            } while (message == "");
            return message;
        }

        private static string ReceiveChatContent(Socket socket)
        {
            string chatContent = SocketUtility.ReceiveString(socket,
                () => { Console.WriteLine($"Receive string size check from server client side exception"); },
                () => { Console.WriteLine($"Receive string data check from server client side exception"); });
            return chatContent;
        }

        private static Socket ConnectClientToServer(IPEndPoint serverEndPoint)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.IP);

            socket.Connect(serverEndPoint);

            Console.WriteLine($"Client connected Local {socket.LocalEndPoint} Remote {socket.RemoteEndPoint}");

            return socket;
        }
    }
}
