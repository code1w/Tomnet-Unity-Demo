using Tom.Core;
using System;
using System.Collections.Generic;

namespace Tom.Logging
{
    public class Logger
    {
        private TomOrange smartFox;

        private bool enableConsoleTrace = true;

        private bool enableEventDispatching = true;

        private LogLevel loggingLevel;

        public bool EnableConsoleTrace
        {
            get
            {
                return enableConsoleTrace;
            }
            set
            {
                enableConsoleTrace = value;
            }
        }

        public bool EnableEventDispatching
        {
            get
            {
                return enableEventDispatching;
            }
            set
            {
                enableEventDispatching = value;
            }
        }

        public LogLevel LoggingLevel
        {
            get
            {
                return loggingLevel;
            }
            set
            {
                loggingLevel = value;
            }
        }

        public Logger(TomOrange smartFox)
        {
            this.smartFox = smartFox;
            loggingLevel = LogLevel.INFO;
        }

        public void Debug(params string[] messages)
        {
            Log(LogLevel.DEBUG, string.Join(" ", messages));
        }

        public void Info(params string[] messages)
        {
            Log(LogLevel.INFO, string.Join(" ", messages));
        }

        public void Warn(params string[] messages)
        {
            Log(LogLevel.WARN, string.Join(" ", messages));
        }

        public void Error(params string[] messages)
        {
            Log(LogLevel.ERROR, string.Join(" ", messages));
        }

        private void Log(LogLevel level, string message)
        {
            if (level >= loggingLevel)
            {
                if (enableConsoleTrace)
                {
                    Console.WriteLine(string.Concat("[SFS - ", level, "] ", message));
                }
                if (enableEventDispatching && smartFox != null)
                {
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("message", message);
                    LoggerEvent evt = new LoggerEvent(level, dictionary);
                    smartFox.DispatchEvent(evt);
                }
            }
        }

        public void AddEventListener(LogLevel level, EventListenerDelegate listener)
        {
            if (smartFox != null)
            {
                smartFox.AddEventListener(LoggerEvent.LogEventType(level), listener);
            }
        }

        public void RemoveEventListener(LogLevel logLevel, EventListenerDelegate listener)
        {
            if (smartFox != null)
            {
                smartFox.RemoveEventListener(LoggerEvent.LogEventType(logLevel), listener);
            }
        }
    }
}
