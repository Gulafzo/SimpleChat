//код С.Дмитрия 
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Utilities
{
    public static class SocketUtility
    {

        public static string ReceiveString(Socket socket)//этомметод  читает данные и возвращает в виде строки также ожидает наличия данных в сокете перед их чтением
        {
            WaitDataFromSocket(socket);
            using (Stream dataStream = new MemoryStream())
            using (BinaryReader dataStreamReader = new BinaryReader(dataStream))
            {
                byte[] dataBuffer = new byte[1024];
                socket.Receive(dataBuffer);
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStream.Write(dataBuffer, 0, sizeof(long));
                dataStream.Seek(0, SeekOrigin.Begin);
                var razmer = dataStreamReader.ReadInt64();
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStream.Write(dataBuffer, 8, Convert.ToInt32(razmer) + 1);
                dataStream.Seek(0, SeekOrigin.Begin);
                return dataStreamReader.ReadString();
            }
        }

        public static string ReceiveString(Socket socket, 
            Action onReceiveDataSizeCheckFail, Action onReceiveDataCheckFail)
        {//это метод  читает данные возвращает их в виде строки и проверяет размер данных, отправленных через сокет, и генерирует сообщение об ошибке,если данные достоверны, читаются в буфер и возвращаются в виде строки
            using (BinaryReader dataStreamReader = new BinaryReader(dataStream))
            {
                var dataSize = ReceiveDataSize(socket, dataStream, dataStreamReader, onReceiveDataSizeCheckFail);
                ReceiveDataToStream(socket, dataSize, dataStream, onReceiveDataCheckFail);
                
                dataStream.Seek(0, SeekOrigin.Begin);
                return dataStreamReader.ReadString();
            }
        }
        
        private static void ReceiveDataToStream(
            Socket socket, long dataSize, 
            Stream dataStream, Action onReceiveDataCheckFail)
        {//это метод  в качестве параметра принимает сокет, размер данных,  поток для записи данных и обратный вызов функции `onReceiveDataCheckFail` в случае возникновения ошибки  
            var maxBufferSize = 1024;
            var remainingDataSize = dataSize;

            dataStream.Seek(0, SeekOrigin.Begin);
            
            while (remainingDataSize > maxBufferSize)
            {
                ReceiveBufferToStream(socket, dataStream, maxBufferSize, onReceiveDataCheckFail);

                remainingDataSize -= maxBufferSize;
            }
            
            ReceiveBufferToStream(socket, dataStream, (int)remainingDataSize, onReceiveDataCheckFail);
        }

        private static void ReceiveBufferToStream(
            Socket socket, Stream dataStream, int bufferSize,
            Action onReceiveDataCheckFail)
        {//это метод  в качестве параметров сокет, поток для записи данных, размер буфера и функцию обратного вызова `onReceiveDataCheckFail`. Метод ожидает появления данных в сокете, читает заданное количество байт и записывает их в указанный поток. Если не удалось считать нужное количество байт, вызывается функция обратного вызова `onReceiveDataCheckFail`.
            WaitDataFromSocket(socket, bufferSize);

            byte[] dataBuffer = new byte[bufferSize];
            var receivedBufferSize = socket.Receive(dataBuffer);

            if (receivedBufferSize != bufferSize)
            {
                onReceiveDataCheckFail();
            }

            dataStream.Write(dataBuffer, 0, bufferSize);
        }

        private static long ReceiveDataSize(Socket socket, Stream dataStream, 
            BinaryReader dataStreamReader, Action onReceiveDataCheckFail)
        {// метод для получения размера данных, вычислятся размер буфера данных и возвращает его. Если полученное  данных не соответствует размеру буфера, то вызывается метод `onReceiveDataCheckFail`.
            WaitDataFromSocket(socket, sizeof(long));

            byte[] dataBuffer = new byte[sizeof(long)];
            var receivedBufferSize = socket.Receive(dataBuffer);

            if (receivedBufferSize != dataBuffer.Length)
            {
                onReceiveDataCheckFail();
            }

            dataStream.Seek(0, SeekOrigin.Begin);
            dataStream.Write(dataBuffer, 0, dataBuffer.Length);
            dataStream.Seek(0, SeekOrigin.Begin);
            return dataStreamReader.ReadInt64();
        }

        public static void WaitDataFromSocket(Socket clientSocket)
        {
            WaitDataFromSocket(clientSocket, 1);
        }

        private static void WaitDataFromSocket(Socket clientSocket, int waitForBytesAvailable)// метод проверяет наличие данных во входящем потоке .Если данных нет, то метод приостанавливает выполнение потока на 100 миллисекунд.
        {
            while (clientSocket.Available < waitForBytesAvailable)
            {
                Thread.Sleep(100);
            }
        }

        public static void SendString(Socket socket, string dataToSend, Action onSendDataCheckFail)
        {//метод для отправки строки через сокет. Данные записываются в временный поток MemoryStream
            using (Stream dataStream = new MemoryStream())
            using (BinaryWriter dataStreamWriter = new BinaryWriter(dataStream))
            {
                /*
                 * записываем пустышку вместо размера пакета данных,
                 * на данном этапе мы не знаем размер отправляемых данных
                 */
                dataStreamWriter.Write((long)0);
                
                dataStreamWriter.Write(dataToSend);
                dataStreamWriter.Flush();
            
                byte[] sendDataBuffer = new byte[dataStream.Position];

                /*
                 * Перезаписываем актуальный размер пакета данных,
                 * теперь мы знаем его размер
                 */
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStreamWriter.Write(dataStream.Length - sizeof(long));
                
                dataStream.Seek(0, SeekOrigin.Begin);
                int readBytesFromMemoryStream = dataStream.Read(sendDataBuffer, 0, sendDataBuffer.Length);

                if (readBytesFromMemoryStream != sendDataBuffer.Length)
                {
                    onSendDataCheckFail();
                }

                socket.Send(sendDataBuffer);
            }
        }

    }
}