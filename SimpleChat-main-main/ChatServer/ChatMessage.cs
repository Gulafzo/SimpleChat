namespace ChatServer
{
    internal class ChatMessage  // это класс  представляет сообщение есть два конструктора: первый сообщение,  второй имя отправитель
    {
        private readonly string _name;
        private readonly string _message;

        public ChatMessage(string message)
        {
            _name = "";
            _message = message;
        }

        public ChatMessage(string name, string message)
        {
            _name= name;
            _message= message;
        }

        public override string ToString()///методвозвращать имю-строку
        {
            if (string.IsNullOrEmpty(_name))
                return _message;
            else
                return $"[{_name}] -- {_message}";
        }
    }
}
