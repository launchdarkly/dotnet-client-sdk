using System;
using System.IO;
using System.Threading.Tasks;
using Xunit.Runners;

// This class is used in both iOS and Android test projects. It is not applicable in .NET Standard.
//
// It is based on the TextWriterResultChannel provided by xunit.runner.devices, but it has better
// diagnostic output for our purposes: captured test output is included for failed tests, and any
// multi-line error messages are logged as individual lines so that our log-parsing logic won't be
// confused by lines with no log prefix.

namespace LaunchDarkly.Sdk.Client.Tests
{
    public class XunitConsoleLoggingResultChannel : IResultChannel
    {
        private readonly TextWriter _writer;
        private readonly object _lock = new object();

        private int _passed, _skipped, _failed;

        public XunitConsoleLoggingResultChannel()
        {
            _writer = Console.Out;
        }

        public Task<bool> OpenChannel(string message = null)
        {
            lock (_lock)
            {
                _failed = _passed = _skipped = 0;
                _writer.WriteLine("[Runner executing:\t{0}]", message);
                return Task.FromResult(true);
            }
        }

        public Task CloseChannel()
        {
            lock (_lock)
            {
                var total = _passed + _failed;
                _writer.WriteLine("Tests run: {0} Passed: {1} Failed: {2} Skipped: {3}", total, _passed, _failed, _skipped);
                return Task.FromResult(true);
            }
        }

        public void RecordResult(TestResultViewModel result)
        {
            lock (_lock)
            {
                switch (result.TestCase.Result)
                {
                    case TestState.Passed:
                        _writer.Write("\t[PASS] ");
                        _passed++;
                        break;
                    case TestState.Skipped:
                        _writer.Write("\t[SKIPPED] ");
                        _skipped++;
                        break;
                    case TestState.Failed:
                        _writer.Write("\t[FAIL] ");
                        _failed++;
                        break;
                    default:
                        _writer.Write("\t[INFO] ");
                        break;
                }
                _writer.Write(result.TestCase.DisplayName);

                var message = result.ErrorMessage;
                if (!string.IsNullOrEmpty(message))
                {
                    _writer.Write(" : {0}", message.Replace("\r\n", "\\r\\n"));
                }
                _writer.WriteLine();

                var stacktrace = result.ErrorStackTrace;
                if (!string.IsNullOrEmpty(result.ErrorStackTrace))
                {
                    WriteMultiLine(result.ErrorStackTrace, "\t\t");
                }
            }

            if (result.HasOutput && result.TestCase.Result != TestState.Passed)
            {
                _writer.WriteLine(">>> test output follows:");
                WriteMultiLine(result.Output, "");
            }
        }

        private void WriteMultiLine(string text, string prefix)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                _writer.WriteLine(prefix + line);
            }
        }
    }
}
