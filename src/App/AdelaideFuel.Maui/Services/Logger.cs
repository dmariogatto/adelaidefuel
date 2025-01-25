using AdelaideFuel.Services;
using System.Net;
using System.Text;

namespace AdelaideFuel.Maui.Services
{
    public class Logger : ILogger
    {
        private const int LogAgeDays = 3;
        private const string LogFileName = "fuel_log.txt";

        private static readonly Lock LogLock = new();

        private readonly IDeviceInfo _deviceInfo;
        private readonly string _logFilePath;

        public Logger(
            IDeviceInfo deviceInfo,
            IFileSystem fileSystem)
        {
            _deviceInfo = deviceInfo;

            _logFilePath = Path.Combine(fileSystem.CacheDirectory, LogFileName);

            if (File.Exists(_logFilePath))
            {
                var createdDate = File.GetCreationTime(_logFilePath).Date;
                if (createdDate.AddDays(LogAgeDays) < DateTime.Today)
                {
                    File.Delete(_logFilePath);
                }
            }
        }

        public void Error(Exception ex, IReadOnlyDictionary<string, string> data = null)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            WriteToLog(ex, string.Empty, data);
        }

        public void Event(string eventName, IReadOnlyDictionary<string, string> properties = null)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            System.Diagnostics.Debug.WriteLine($"Tracking Event: {eventName}");
            return;

#if !DEBUG && SENTRY
#pragma warning disable CS0162 // Unreachable code detected
            if (DeviceInfo.DeviceType == DeviceType.Virtual)
            {
                SentrySdk.CaptureMessage(eventName, scope =>
                {
                    if (properties is not null)
                    {
                        foreach (var d in properties)
                            scope.SetExtra(d.Key, d.Value);
                    }
                });
            }
#pragma warning restore CS0162 // Unreachable code detected
#endif
        }

        public bool ShouldLogException(Exception ex)
        {
            if (ex is null)
                return false;

            switch (ex)
            {
                case TaskCanceledException _:
                case TimeoutException _:
                case OperationCanceledException _:
                case HttpRequestException httpRequestEx
                        when httpRequestEx.Message.Contains("The request timed out", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("No such host is known", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("The network connection was lost", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("Network subsystem is down", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("A server with the specified hostname could not be found", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("The Internet connection appears to be offline", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("Could not connect to the server", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("Connection failure", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("An SSL error has occurred and a secure connection to the server cannot be made", StringComparison.Ordinal) ||
                             httpRequestEx.Message.Contains("net_http_content_stream_copy_error", StringComparison.Ordinal):
                case WebException webEx
                        when webEx.Message.Contains("Canceled", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Socket closed", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Socket is closed", StringComparison.Ordinal) ||
                             webEx.Message.Contains("No address associated with hostname", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Software caused connection abort", StringComparison.Ordinal) ||
                             webEx.Message.Contains("unexpected end of stream", StringComparison.Ordinal):
                case IOException ioEx
                        when ioEx.Message.Contains("Network subsystem is down", StringComparison.Ordinal):
                default:
                    return true;
            }
        }

        public string LogFilePath() => _logFilePath;

        public long LogInBytes()
        {
            var size = 0L;

            if (File.Exists(_logFilePath))
            {
                try
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    size = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return size;
        }

        public async Task<string> GetLogAsync()
        {
            var result = string.Empty;

            if (File.Exists(_logFilePath))
            {
                try
                {
                    using var stream = File.OpenRead(_logFilePath);
                    using var reader = new StreamReader(stream);
                    result = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return result;
        }

        public void DeleteLog()
        {
            if (File.Exists(_logFilePath))
            {
                try
                {
                    lock (LogLock)
                    {
                        File.Delete(_logFilePath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        private void WriteToLog(Exception ex, string msg, IReadOnlyDictionary<string, string> data)
        {
            if (!ShouldLogException(ex))
                return;

#if SENTRY
            if (_deviceInfo.DeviceType != DeviceType.Virtual)
            {
                SentrySdk.CaptureException(ex, scope =>
                {
                    if (data is not null)
                    {
                        foreach (var d in data)
                            scope.SetExtra(d.Key, d.Value);
                    }
                });
            }
#endif

            var sb = new StringBuilder();
            sb.Append($"[{DateTime.UtcNow:o}]");

            if (!string.IsNullOrWhiteSpace(msg))
                sb.Append($" {msg}");

            sb.AppendLine();

            if (ex is not null)
                sb.AppendLine(ex.ToString());

            var innerEx = ex.InnerException;
            while (innerEx is not null)
            {
                sb.AppendLine(ex.ToString());
                innerEx = innerEx.InnerException;
            }

            data?.ForEach(d => sb.AppendLine($"    {d.Key} : {d.Value}"));

            sb.AppendLine();

            lock (LogLock)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
    }
}