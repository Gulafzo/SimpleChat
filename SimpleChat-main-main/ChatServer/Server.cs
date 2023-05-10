using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatServer.EventsArgs;
using Utilities;

namespace ChatServer
{
    internal class Server : IDisposable
    {
        private const int MAX_CLIENTS_WAITING_FOR_CONNECT = 5;

        public event EventHandler<Exception> AcceptClientException;
        public event EventHandler<ClientSocketExceptionArgs> SendDataToClientException;
        public event EventHandler<ClientSocketExceptionArgs> ReceiveDataFromClientException;
        public event EventHandler<ClientSocketEventArgs> ChatContentSentToClient;
        public event EventHandler<ServerSocketEventArgs> WaitingForClientConnect;
        public event EventHandler<ClientSocketEventArgs> WaitingDataFromClient;
        public event EventHandler<ClientConnectedArgs> ClientConnected;
        public event EventHandler<string> ClientMessageReceived;
        public event EventHandler<ClientSocketEventArgs> ClientDisconnected;

        private int _serverPort;
        private Socket _serverSocket;
        private bool _isServerAlive;

        public static Server Initialise(int listeningPort)
        {
            return new Server(listeningPort);
        }

        private Server(int serverPort)
        {
            _serverPort = serverPort;
        }

        public void Start()
        {
            _serverSocket = new Socket(SocketType.Stream, ProtocolType.IP);
            try
            {
                var ipAdress = IPAddress.Parse(IpAddressUtility.GetLocalAddress());
                _serverSocket.Bind(new IPEndPoint(ipAdress, _serverPort));
                _serverSocket.Listen(MAX_CLIENTS_WAITING_FOR_CONNECT);
                ServerLocator.Port = _serverPort;
            }
            catch{ 
                _serverPort++;
                Start();
                return;
            }
            _isServerAlive = true;

            StartClientTask();
        }

        private void StartClientTask()
        {
            Task.Run(() => ClientWorker());
        }

        public void Stop()
        {
            _isServerAlive = false;
            _serverSocket.Close();
        }

        public void Dispose()
        {
            Stop();
            _serverSocket?.Dispose();
        }

        private void ClientWorker()
        {
            Socket clientSocket = AcceptClient();

            StartClientTask();

            SendChatContentToClient(clientSocket);
            WaitForDataFromClientAvailable(clientSocket);
            var name = ReceiveChatMessageFromClient(clientSocket);
            Sender.AddClient(clientSocket);
            do
            {
                WaitForDataFromClientAvailable(clientSocket);
                var chatMessage = ReceiveChatMessageFromClient(clientSocket);
                if (chatMessage == "")
                    break;
                ChatDatabase.AddMessage(chatMessage, name);
                Sender.SendMessage(chatMessage, name ,clientSocket);
            } while (_isServerAlive);
            Sender.RemoveClient(clientSocket);
            DisconnectClient(clientSocket);
        }

        private void DisconnectClient(Socket clientSocket)
        {
            clientSocket.Disconnect(false);
            ClientDisconnected?.Invoke(this, ClientSocketEventArgs.Create(clientSocket));
            clientSocket.Close();
            clientSocket.Dispose();
        }

        private string ReceiveChatMessageFromClient(Socket clientSocket)
        {
            var chatMessage = SocketUtility.ReceiveString(clientSocket, () =>
                {
                    ReceiveDataFromClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Retrieving string size from socket check fail"),
                            clientSocket
                        )
                    );
                },
                () =>
                {
                    ReceiveDataFromClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Retrieving string from socket check fail"),
                            clientSocket
                        )
                    );
                });
            ClientMessageReceived?.Invoke(this, chatMessage);
            return chatMessage;
        }

        private void WaitForDataFromClientAvailable(Socket clientSocket)
        {
            WaitingDataFromClient?.Invoke(this, ClientSocketEventArgs.Create(clientSocket));
            SocketUtility.WaitDataFromSocket(clientSocket);
        }

        private void SendChatContentToClient(Socket clientSocket)//метод SendChatContentToClient отправляет клиенту состояние чата, используя метод SocketUtility.SendString для отправки сообщения в виде строки.
        {
            SocketUtility.SendString(clientSocket, ChatDatabase.GetChat(),
                () =>
                {
                    SendDataToClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Preparation data for socket send check fail"),
                            clientSocket
                        )
                    );
                });
            ChatContentSentToClient?.Invoke(this, ClientSocketEventArgs.Create(clientSocket));
        }

        private Socket AcceptClient()//метод AcceptClient -когда клиент подкл.  вызывает WaitingForClientConnect для уведомления ожидающих объектов, если клиент подключен, то возвращает  клиентский сокет.
        {

            Socket clientSocket = null;

            WaitingForClientConnect?.Invoke(this, ServerSocketEventArgs.Create(_serverSocket));

            try
            {
                clientSocket = _serverSocket.Accept();
            }
            catch (SocketException ex)
            {
                AcceptClientException?.Invoke(this, ex);
            }
            catch (ObjectDisposedException ex)
            {
                AcceptClientException?.Invoke(this, ex);
            }
            catch (InvalidOperationException ex)
            {
                AcceptClientException?.Invoke(this, ex);
            }

            ClientConnected?.Invoke(this, ClientConnectedArgs.Create(_serverSocket, clientSocket));

            return clientSocket;
        }
    }
}
