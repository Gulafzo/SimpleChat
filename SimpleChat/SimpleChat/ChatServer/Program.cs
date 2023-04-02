using System;
using System.Net;
using ChatServer.EventsArgs;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var server = Server.Initialise(10111))// Используем конструкцию using для автоматического освобождения ресурсов сервера
            { // Подписываемся на события сервера
                server.AcceptClientException += Server_AcceptClientException;
                server.WaitingForClientConnect += Server_WaitingForClientConnect;
                server.ClientConnected += Server_ClientConnected;
                server.SendDataToClientException += Server_SendDataToClientException;
                server.ChatContentSentToClient += Server_ChatContentSentToClient;
                server.WaitingDataFromClient += Server_WaitingDataFromClient;
                server.ReceiveDataFromClientException += Server_ReceiveDataFromClientException;
                server.ClientMessageReceived += ServerClientMessageReceived;
                server.ClientDisconnected += Server_ClientDisconnected;
                server.Start();

                Console.ReadLine();//  ввод пользователем
                server.Stop();//стоп сервер
            }
        }

       
        private static void Server_ClientDisconnected(object sender, ClientSocketEventArgs e) // Обработчик отключения клиента
        {
            Console.WriteLine("Client with " +
                              $"local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " +
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)} disconnected.");
        }

        
        private static void ServerClientMessageReceived(object sender, string e)// Обработчик получения сообщения от клиента
        {
            Console.WriteLine($"Client message [{e}] received");
        }

        
        private static void Server_ReceiveDataFromClientException(object sender, ClientSocketExceptionArgs e)// Обработчик исключений при получении данных от клиента
        {
            Console.WriteLine("Receiving data from client with " +
                              $"local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " +
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)} " +
                              $"caused exception [{e.Exception.Message}] on server side.");
        }

        // Обработчик ожидания данных от клиента
        private static void Server_WaitingDataFromClient(object sender, ClientSocketEventArgs e)
        {
            Console.WriteLine("Waiting data from client with " + // выводим сообщение ожидания данных от клиента
                              $"local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " + // выводим локальный IPv4 адрес клиента
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)}."); // выводим удаленный IPv4 адрес клиента
        }

        // Обработчик отправки сообщения чата клиенту
        private static void Server_ChatContentSentToClient(object sender, ClientSocketEventArgs e)
        {
            Console.WriteLine("Chat content sent to client with " + // выводим сообщение об отправке сообщения чата клиенту
                              $"local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " + // выводим локальный IPv4 адрес клиента
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)}."); // выводим удаленный IPv4 адрес клиента
        }

        // Обработчик исключений при отправке данных клиенту
        private static void Server_SendDataToClientException(object sender, ClientSocketExceptionArgs e)
        {
            Console.WriteLine("Send data to client with " + // выводим сообщение об отправке данных клиенту
                              $"local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " + // выводим локальный IPv4 адрес клиента
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)} " + // выводим удаленный IPv4 адрес клиента
                              $"caused exception [{e.Exception.Message}] on server side."); // выводим сообщение об исключении на стороне сервера
        }

        // Обработчик подключения клиента
        private static void Server_ClientConnected(object sender, ClientConnectedArgs e)
        {
            Console.WriteLine($"Client with local IP v4 address {GetIpV4Address(e.ClientSocket.LocalEndPoint)} " + // выводим локальный IPv4 адрес клиента
                              $"and remote IP v4 address {GetIpV4Address(e.ClientSocket.RemoteEndPoint)} " + // выводим удаленный IPv4 адрес клиента
                              $"connected to server with local IP v4 address {GetIpV4Address(e.ServerSocket.LocalEndPoint)}."); // выводим локальный IPv4 адрес сервера
        }
  
        private static string GetIpV4Address(EndPoint endPoint)// Вспомогательный метод для получения IPv4 адреса
        {
            var ipEndPoint = (IPEndPoint)endPoint;
            var ip = ipEndPoint.Address.MapToIPv4().ToString();
            var port = ipEndPoint.Port;
            return $"[{ip}]:{port}";
        }

        private static void Server_WaitingForClientConnect(object sender, ServerSocketEventArgs e)// Обработчик ожидания подключения клиента
        {
            Console.WriteLine($"Server with local IP v4 address {GetIpV4Address(e.ServerSocket.LocalEndPoint)} " +
                              "waiting for client connection.");
        }

        private static void Server_AcceptClientException(object sender, Exception e)// Обработчик исключений при подключении клиента
        {
            Console.WriteLine($"Server caused exception while client accept [{e.Message}].");
        }
    }
}










