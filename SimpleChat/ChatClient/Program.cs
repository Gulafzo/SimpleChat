using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Utilities;

namespace ChatClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var socket = ConnectClientToServer(new IPEndPoint(IPAddress.Loopback, 10111));// создание объекта сокета и подключение к серверу по  адресу и порту

            var chatContent = ReceiveChatContent(socket);// получение данных чата от сервера

            ShowChatContent(chatContent);// отображение полученных данных чата на экране

            var message = GetClientMessage();//  сообщения от пользователя

            SendMessageToServer(socket, message);// отправка сообщения на сервер

            /*
             * Потенциально будет нужна в ходе дальнейшей разработки
             * В текущей версии строку ожидания Enter заменяет ожидание в
             * 1 секунду ниже
             */
            //WaitForEnterPressedToCloseApplication();

            DisconnectClientFromServer(socket);
            
            Thread.Sleep(TimeSpan.FromSeconds(1));// ожидание 1 сек. перед завершением работы 

            DisposeClientSocket(socket);// тключение 
        }

        private static void DisposeClientSocket(Socket socket) //закрывает и освобождает ресурсы
        {
            socket.Close();
            socket.Dispose();
        }

        private static void DisconnectClientFromServer(Socket socket)Метод `DisconnectClientFromServer(Socket socket)` // метод отключает клиентский сокет от сервера и выводит сообщение 
        {
            socket.Disconnect(false);
            Console.WriteLine("Client disconnected from server");
        }

        private static void WaitForEnterPressedToCloseApplication()// метод ожидает, пока пользователь не нажмет клавишу Enter, и затем закрывает консольное приложение клиента
        {
            Console.Write("Press [Enter] to close client console application");
            Console.ReadLine();
        }

        private static void SendMessageToServer(Socket socket, string message)// метод отправляет сообщение на сервер
        {
            Console.WriteLine("Sending message to server");
            SocketUtility.SendString(socket, message,           //Если возникла ошибка при отправке , метод SendString вызывает лямбда-выражение для обработки ошибки 
                () => { Console.WriteLine($"Send string to server data check client side exception"); });
            Console.WriteLine("Message sent to server");
        }

        private static string GetClientMessage()//  метод GetClientMessage
    {
            Console.Write("Your message:"); 
            var message = Console.ReadLine();
            return message;//  возвращает сообщению его как строку
        }

        private static void ShowChatContent(string chatContent)//  метод отображает чат в консоль
    {
            Console.WriteLine("---------------Chat content--------------------");
            Console.WriteLine(chatContent);
            Console.WriteLine("------------End of chat content----------------");
            Console.WriteLine();
        }

        private static string ReceiveChatContent(Socket socket)//  метод который получает  чат от сервера с помощью сокета
    {           // получение чата как строка  и обработка ошибок с помощью лямбда-выражений
                string chatContent = SocketUtility.ReceiveString(socket,
                () => { Console.WriteLine($"Receive string size check from server client side exception"); },
                () => { Console.WriteLine($"Receive string data check from server client side exception"); });
            return chatContent;
        }

        private static Socket ConnectClientToServer(IPEndPoint serverEndPoint)
        {//  закрытый метод,  соединяет клиента с сервером по  IP-адресу и порту
        Socket socket = new Socket(SocketType.Stream, ProtocolType.IP);
            
            socket.Connect(serverEndPoint);

            Console.WriteLine($"Client connected Local {socket.LocalEndPoint} Remote {socket.RemoteEndPoint}");
            
            return socket;
        }
    }
}
