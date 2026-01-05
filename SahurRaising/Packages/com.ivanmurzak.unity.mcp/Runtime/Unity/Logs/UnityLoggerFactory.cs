using System;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP
{
    public static class UnityLoggerFactory
    {
        public static ILoggerFactory LoggerFactory { get; } = new SimpleUnityLoggerFactory();

        private class SimpleUnityLoggerFactory : ILoggerFactory
        {
            public void AddProvider(ILoggerProvider provider) { }
            public ILogger CreateLogger(string categoryName) => new SimpleUnityLogger(categoryName);
            public void Dispose() { }
        }

        private class SimpleUnityLogger : ILogger
        {
            private readonly string _categoryName;
            public SimpleUnityLogger(string categoryName) => _categoryName = categoryName;
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var msg = formatter(state, exception);
                var logMsg = $"[{_categoryName}] {msg}";
                if (exception != null) logMsg += $"\n{exception}";

                switch (logLevel)
                {
                    case Microsoft.Extensions.Logging.LogLevel.Trace:
                    case Microsoft.Extensions.Logging.LogLevel.Debug:
                    case Microsoft.Extensions.Logging.LogLevel.Information:
                        Debug.Log(logMsg);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Warning:
                        Debug.LogWarning(logMsg);
                        break;
                    case Microsoft.Extensions.Logging.LogLevel.Error:
                    case Microsoft.Extensions.Logging.LogLevel.Critical:
                        Debug.LogError(logMsg);
                        break;
                }
            }
        }
    }

    public class UnityLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => UnityLoggerFactory.LoggerFactory.CreateLogger(categoryName);
        public void Dispose() { }
    }
}
