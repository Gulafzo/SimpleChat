using System.Collections.Generic;
using System.Linq;

namespace ChatServer
{
    internal static class ChatDatabase//  класс ChatDatabase
    {
        private static List<string> _chatLines = new List<string>() //  список строк
        {
            "Welcome to our best chat ever:", 
            "----------------------------",
            "Enter your joke now:", 
            "----------------------------",
        };

        public static void AddMessage(string message) //  метод , который добавляет новое сообщение в  _chatLines
        {
            _chatLines.Add(message);
        }

        public static string GetChat()  //  метод  возвращает  сообщения из списка
        {
            return _chatLines
                .Aggregate("", (accumulate, line) => $"{accumulate}\n{line}")
                .TrimStart('\n');
        }
    }
}
