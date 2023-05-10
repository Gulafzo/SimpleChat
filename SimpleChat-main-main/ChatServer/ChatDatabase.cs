using System.Collections.Generic;
using System.Linq;

namespace ChatServer
{
    internal static class ChatDatabase//Это класс представляет базу данных для  сообщений  
    {
        private static List<ChatMessage> _chatLines = new List<ChatMessage>()//список объектов ChatMessage 
        {
            new ChatMessage("Welcome to our best chat ever:"),
            new ChatMessage("----------------------------"),
            new ChatMessage("Enter your joke now:"),
            new ChatMessage("----------------------------"),
        };

        public static void AddMessage(string message, string name)//метод добавления новых сообщений
        {
            _chatLines.Add(new ChatMessage(name, message));
        }

        public static string GetChat()//метод для получения всего списка сообщений
    {
            return _chatLines
                .Aggregate("", (accumulate, line) => $"{accumulate}\n{line}")
                .TrimStart('\n');
        }
    }
}
