using System.Collections.Generic;
using System.Net.Sockets;
using Utilities;

namespace ChatServer
{
    internal static class Sender  //в этом классе есть методы  добавления и удаления клиента , отправки сообщений всем клиентам, подключенным к серверу
    {
        static private readonly object _socketLinesLockObj = new object();
        private static readonly List<Socket> _socketLines = new List<Socket>();

        public static void AddClient(Socket socket)
        {
            lock (_socketLinesLockObj)
            {
                _socketLines.Add(socket);
            }
        }

        public static void RemoveClient(Socket socket)
        {
            lock (_socketLinesLockObj)
            {
                _socketLines.Remove(socket);
            }
        }

        public static void SendMessage(string message, string name, Socket socket)
        {
            lock (_socketLinesLockObj)
            {
                for (int i = 0; i < _socketLines.Count; i++)
                {
                    if (_socketLines[i] != socket)
                    {
                        string stroka = new ChatMessage(name, message).ToString();
                        SocketUtility.SendString(_socketLines[i], stroka, () => { });
                    }
                }
            }
        }
    }
}
