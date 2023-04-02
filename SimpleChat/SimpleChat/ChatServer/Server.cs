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
        private const int MAX_CLIENTS_WAITING_FOR_CONNECT = 5;// Максимальное количество клиентов, ожидающих подключения

        public event EventHandler<Exception> AcceptClientException;
        public event EventHandler<ClientSocketExceptionArgs> SendDataToClientException;
        public event EventHandler<ClientSocketExceptionArgs> ReceiveDataFromClientException;
        public event EventHandler<ClientSocketEventArgs> ChatContentSentToClient;
        public event EventHandler<ServerSocketEventArgs> WaitingForClientConnect;
        public event EventHandler<ClientSocketEventArgs> WaitingDataFromClient;
        public event EventHandler<ClientConnectedArgs> ClientConnected;
        public event EventHandler<string> ClientMessageReceived;
        public event EventHandler<ClientSocketEventArgs> ClientDisconnected;

        private int _serverPort;// Порт на котором работает сервер
        private Socket _serverSocket;// Сокет
        private bool _isServerAlive;// работает ли сервер в данный момент

        public static Server Initialise(int listeningPort)// метод для инициализации сервера
        {
            return new Server(listeningPort);
        }

        private Server(int serverPort)// конструктор  Server, принимающий порт сервера
        {
            _serverPort = serverPort;
        }

        public void Start()// метод для запуска сервера
        {
            _serverSocket = new Socket(SocketType.Stream, ProtocolType.IP);// Создание сокета 
            _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, _serverPort)); // привязка сокета к локальному адресу и  порту
            _serverSocket.Listen(MAX_CLIENTS_WAITING_FOR_CONNECT);// количества клиентов, которые могут ожидать подключения к серверу
            _isServerAlive = true;//сервер запущен

            StartClientTask(); // обработка подкл клиентов
        }

        private void StartClientTask()// Метод для обработки подкл клиентов
        {
            Task.Run(() => ClientWorker());
        }

        public void Stop()// Метод  остановки сервера
        {
            _isServerAlive = false;
            _serverSocket.Close();
        }

        public void Dispose()// освобождения ресурсов, используемых сервером
        {
            Stop();
            _serverSocket?.Dispose();
        }

        private void ClientWorker()
        {
            Socket clientSocket = AcceptClient(); // принимаем подключение от клиента

            StartClientTask();    // запуск задач для обработки следующего подключения

            SendChatContentToClient(clientSocket);   // отправ. содержимое чата клиенту
            WaitForDataFromClientAvailable(clientSocket);   // ожидание доступных данных от клиента
            var chatMessage = ReceiveChatMessageFromClient(clientSocket);  // получение сообщение от клиента и добав.его в базу данных чата
            ChatDatabase.AddMessage(chatMessage);

            DisconnectClient(clientSocket);//отклю.клиента
        }
        
        private void DisconnectClient(Socket clientSocket)// метод для отключения клиента
        {            
            clientSocket.Disconnect(false);// Разрыв соединение с клиентом     
            ClientDisconnected?.Invoke(this, ClientSocketEventArgs.Create(clientSocket)); //  клиент отключился            
            clientSocket.Close();//  сокет закрыт
            clientSocket.Dispose();
        }
       
        private string ReceiveChatMessageFromClient(Socket clientSocket) // метод для получения сообщения от клиента через сокет
        {
            var chatMessage = SocketUtility.ReceiveString(clientSocket, () =>// получение строку сообщения от клиента 
                {// обработчик исключения
                    ReceiveDataFromClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Retrieving string size from socket check fail"),
                            clientSocket
                        )
                    );
                },
                () =>
                { // Обработчик исключения
                    ReceiveDataFromClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Retrieving string from socket check fail"),
                            clientSocket
                        )
                    );
                });
            ClientMessageReceived?.Invoke(this, chatMessage);// Вызыв событие о получении сообщения от клиента
            return chatMessage; // возвращение полученное сообщение
        }

        private void WaitForDataFromClientAvailable(Socket clientSocket)// метод  ожидания доступных данных от клиента 
        {
            WaitingDataFromClient?.Invoke(this, ClientSocketEventArgs.Create(clientSocket)); // вызыв событие ожидания данных
            SocketUtility.WaitDataFromSocket(clientSocket);//   данные от клиента
        }

        private void SendChatContentToClient(Socket clientSocket) // метод для отправки сообдений
        {
            SocketUtility.SendString(clientSocket, ChatDatabase.GetChat(),// отправка содержимое чата клиенту
                // Обработчик исключения
                () =>
                {// вызов событие об ошибке при отправке данных клиенту
                    SendDataToClientException?.Invoke(this,
                        ClientSocketExceptionArgs.Create(
                            new Exception("Preparation data for socket send check fail"),
                            clientSocket
                        )
                    );
                });
            ChatContentSentToClient?.Invoke(this, ClientSocketEventArgs.Create(clientSocket));//  содержимое чата отправлено клиенту
        }

        private Socket AcceptClient()// метод для принятия клиента
        {

            Socket clientSocket = null;

            WaitingForClientConnect?.Invoke(this, ServerSocketEventArgs.Create(_serverSocket));//  ожидания подключения клиента

            try
            {
                clientSocket = _serverSocket.Accept();// принимаем клиента через серверный сокет
            }
            catch (SocketException ex)
            {
                AcceptClientException?.Invoke(this, ex);// событие об ошибке 
            }
            catch (ObjectDisposedException ex)
            {
                AcceptClientException?.Invoke(this, ex);// событие об ошибке 
            }
            catch (InvalidOperationException ex)
            {
                AcceptClientException?.Invoke(this, ex);// событие об ошибке 
            }

            ClientConnected?.Invoke(this, ClientConnectedArgs.Create(_serverSocket, clientSocket));// подключении клиента

            return clientSocket;
        }
    }
}

