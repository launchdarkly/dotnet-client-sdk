using System;
using System.Collections.Generic;
using Common.Logging;
using Common.Logging.Simple;

namespace LaunchDarkly.Xamarin.Tests
{
    // This mechanism allows unit tests to capture and inspect log output. In order for it to work properly, all
    // tests must derive from BaseTest so that the global Common.Logging configuration is modified before any
    // test code runs (i.e. before any SDK code has a change to create a logger instance).
    //
    // For debugging purposes, this also allows you to mirror all log output to the console (which Common.Logging
    // does not do by default) by setting the environment variable LD_TEST_LOGS to any value.

    public class LogSink : AbstractSimpleLogger
    {
        public static LogSink Instance = new LogSink();

        private static readonly bool _showLogs = Environment.GetEnvironmentVariable("LD_TEST_LOGS") != null;

        public LogSink() : base("", LogLevel.All, false, false, false, "") {}

        protected override void WriteInternal(LogLevel level, object message, Exception exception)
        {
            var str = message?.ToString();
            try
            {
                LogSinkFactoryAdapter._logFn?.Invoke(DateTime.Now.ToString() + " [" + level + "] " + str);
            }
            catch { }
            if (_showLogs)
            {
                Console.WriteLine("*** LOG: [" + level + "] " + str);
            }
            LogSinkScope.WithCurrent(s => s.Messages.Add(new LogItem { Level = level, Text = str }));
        }
    }

    public struct LogItem
    {
        public LogLevel Level { get; set; }
        public string Text { get; set; }
    }

    public class LogSinkFactoryAdapter : AbstractSimpleLoggerFactoryAdapter
    {
        public static Action<string> _logFn;

        public LogSinkFactoryAdapter() : this(null) { }

        public LogSinkFactoryAdapter(Action<string> logFn) : base(null)
        {
            _logFn = logFn;
        }

        protected override ILog CreateLogger(string name, LogLevel level, bool showLevel, bool showDateTime, bool showLogName, string dateTimeFormat)
        {
            return LogSink.Instance;
        }
    }

    public class LogSinkScope : IDisposable
    {
        private static Stack<LogSinkScope> _scopes = new Stack<LogSinkScope>();

        public List<LogItem> Messages = new List<LogItem>();

        public LogSinkScope()
        {
            _scopes.Push(this);
        }

        public void Dispose()
        {
            _scopes.Pop();
        }

        public static void WithCurrent(Action<LogSinkScope> a)
        {
            if (_scopes.TryPeek(out var s))
            {
                a(s);
            }
        }
    }
}
