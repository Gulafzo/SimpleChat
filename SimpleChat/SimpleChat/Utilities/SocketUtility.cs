using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Utilities
{
    public static class SocketUtility
    {
        public static string ReceiveString(Socket socket,  Action onReceiveDataSizeCheckFail, Action onReceiveDataCheckFail)
        // Метод для принятия строки от сокета
        {
            using (Stream dataStream = new MemoryStream())                        // Создание потока данных 
            using (BinaryReader dataStreamReader = new BinaryReader(dataStream))  //для чтения данных из потока
            {
                var dataSize = ReceiveDataSize(socket, dataStream, dataStreamReader, onReceiveDataSizeCheckFail);
                // Получение размера данных,  вызов метода ReceiveDataSize
                ReceiveDataToStream(socket, dataSize, dataStream, onReceiveDataCheckFail);  // Принятие данных в пток

                dataStream.Seek(0, SeekOrigin.Begin); // перемещение указателя потока в нчало
                return dataStreamReader.ReadString();  // чтение строки из потока с помощью бинарного ридера
            }
        }
        
        private static void ReceiveDataToStream(
            Socket socket, long dataSize, 
            Stream dataStream, Action onReceiveDataCheckFail)// Метод  принимает данных из сокета в поток
        {
            var maxBufferSize = 1024; // максимальный размер буфера для чтения данных
            var remainingDataSize = dataSize; // оставшийся размер данных 

            dataStream.Seek(0, SeekOrigin.Begin);// перемещаем указатель потока в начало


            while (remainingDataSize > maxBufferSize)// цикл для чтения данных
            {
                ReceiveBufferToStream(socket, dataStream, maxBufferSize, onReceiveDataCheckFail);// вызов метода для чтения буфера из сокета в поток

                remainingDataSize -= maxBufferSize;// Уменьшение оставшийся размер данных 
            }
            
            ReceiveBufferToStream(socket, dataStream, (int)remainingDataSize, onReceiveDataCheckFail);// Читение оставшиеся данные
        }

        private static void ReceiveBufferToStream(
            Socket socket, Stream dataStream, int bufferSize,
            Action onReceiveDataCheckFail)// Метод для чтения буфера данных из сокеа в поток
        {
            WaitDataFromSocket(socket, bufferSize);

            byte[] dataBuffer = new byte[bufferSize];// буфер для принятых данных

            var receivedBufferSize = socket.Receive(dataBuffer); //  данные сохраняем в буфер

            if (receivedBufferSize != bufferSize) // Проверяем, что  размер данных соответствует ожидаемому размеру
            {
                onReceiveDataCheckFail(); // иызываем метод, если размер данных не соответствует 
            }

            dataStream.Write(dataBuffer, 0, bufferSize);//   записываем  данные в поток
        }

        private static long ReceiveDataSize(Socket socket, Stream dataStream, // метод для чтения
            BinaryReader dataStreamReader, Action onReceiveDataCheckFail)
        {
            WaitDataFromSocket(socket, sizeof(long));  // ожидаем прихода данных

            byte[] dataBuffer = new byte[sizeof(long)];// буфер для данных
            var receivedBufferSize = socket.Receive(dataBuffer); // данные сохраняем  в буфер

            if (receivedBufferSize != dataBuffer.Length)  // проверка размера данных 
            {
                onReceiveDataCheckFail(); // Вызываем  метод onReceiveDataCheckFail
             
            }

            dataStream.Seek(0, SeekOrigin.Begin);// Записываем данные в поток
            dataStream.Write(dataBuffer, 0, dataBuffer.Length);
            dataStream.Seek(0, SeekOrigin.Begin);
            return dataStreamReader.ReadInt64();// Читаем размер данных из потока
        }

        public static void WaitDataFromSocket(Socket clientSocket)//  открытый статический метод, принимает  Socket в качестве параметра
        {
            WaitDataFromSocket(clientSocket, 1); // вызыв метода WaitDataFromSocket с тайм-аутом в 1 миллисекунду
        }

        private static void WaitDataFromSocket(Socket clientSocket, int waitForBytesAvailable)//  закрытый статический метод в качестве параметра принимает Socket и значение waitForBytesAvailable                                                                                          
        {
            while (clientSocket.Available < waitForBytesAvailable)  //наличия данных в сокете
            {
                Thread.Sleep(100);  // если данных нет, поток засыпает на 100 миллисекунд
            }
        }

        public static void SendString(Socket socket, string dataToSend, Action onSendDataCheckFail)// статический метод принимает  Socket ,строку и Action как параметр
        {
            using (Stream dataStream = new MemoryStream())// блок using для освобождения ресурсов после использования, объект MemoryStream
                                                          
            using (BinaryWriter dataStreamWriter = new BinaryWriter(dataStream))  // объект BinaryWriter и передаем  dataStream
            {
                /*
                 * записываем пустышку вместо размера пакета данных,
                 * на данном этапе мы не знаем размер отправляемых данных
                 */
                dataStreamWriter.Write((long)0); 

                dataStreamWriter.Write(dataToSend); // записываем данные для отправки
                dataStreamWriter.Flush();// очищаем буфер
            
                byte[] sendDataBuffer = new byte[dataStream.Position]; // Создаем буфер отправляемых данных

                /*
                 * Перезаписываем актуальный размер пакета данных,
                 * теперь мы знаем его размер
                 */
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStreamWriter.Write(dataStream.Length - sizeof(long));

                dataStream.Seek(0, SeekOrigin.Begin);  // сбрасываем позицию потока в начало
                int readBytesFromMemoryStream = dataStream.Read(sendDataBuffer, 0, sendDataBuffer.Length);// читаем данные из потока

                if (readBytesFromMemoryStream != sendDataBuffer.Length)// если количество прочитанных байт не равно размеру буфера, вызываем onSendDataCheckFail
                {
                    onSendDataCheckFail();
                }
                // Отправка данные через сокет
                socket.Send(sendDataBuffer);
            }
        }

    }
}