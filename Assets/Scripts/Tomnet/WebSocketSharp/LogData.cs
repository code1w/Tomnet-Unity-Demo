using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WebSocketSharp
{
    public class LogData
    {
        private StackFrame _caller;

        private DateTime _date;

        private LogLevel _level;

        private string _message;

        public StackFrame Caller => _caller;

        public DateTime Date => _date;

        public LogLevel Level => _level;

        public string Message => _message;

        internal LogData(LogLevel level, StackFrame caller, string message)
        {
            _level = level;
            _caller = caller;
            _message = (message ?? string.Empty);
            _date = DateTime.Now;
        }

        public override string ToString()
        {
            string text = string.Format("{0}|{1,-5}|", _date, _level);
            MethodBase method = _caller.GetMethod();
            Type declaringType = method.DeclaringType;
            int fileLineNumber = _caller.GetFileLineNumber();
            string arg = $"{text}{declaringType.Name}.{method.Name}:{fileLineNumber}|";
            string[] array = _message.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            if (array.Length <= 1)
            {
                return $"{arg}{_message}";
            }
            StringBuilder stringBuilder = new StringBuilder($"{arg}{array[0]}\n", 64);
            string format = $"{{0,{text.Length}}}{{1}}\n";
            for (int i = 1; i < array.Length; i++)
            {
                stringBuilder.AppendFormat(format, "", array[i]);
            }
            stringBuilder.Length--;
            return stringBuilder.ToString();
        }
    }
}
