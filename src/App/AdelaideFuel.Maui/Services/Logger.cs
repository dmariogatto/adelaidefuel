using AdelaideFuel.Services;
using System.Net;

namespace AdelaideFuel.Maui.Services
{
    public class Logger : ILogger
    {
        private const string LogFileName = "fuel_log.txt";

        private static readonly object LogLock = new object();

        private readonly IDeviceInfo _deviceInfo;
        private readonly string _logFilePath;

        public Logger(
            IDeviceInfo deviceInfo,
            IFileSystem fileSystem)
        {
            _deviceInfo = deviceInfo;

            _logFilePath = Path.Combine(fileSystem.CacheDirectory, LogFileName);
        }

        public void Error(Exception ex, IReadOnlyDictionary<string, string> data = null)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            WriteToLog(ex, string.Empty, data);
        }

        public void Event(string eventName, IReadOnlyDictionary<string, string> properties = null)
        {
            if (!string.IsNullOrWhiteSpace(eventName))
            {
                System.Diagnostics.Debug.WriteLine($"Tracking Event: {eventName}");
                return;

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
            }
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
                case HttpRequestException httpRequstEx
                        when httpRequstEx.Message.Contains("No such host is known", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("The network connection was lost", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("Network subsystem is down", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("A server with the specified hostname could not be found", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("The Internet connection appears to be offline", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("Could not connect to the server", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("Connection failure", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("An SSL error has occurred and a secure connection to the server cannot be made", StringComparison.Ordinal) ||
                             httpRequstEx.Message.Contains("net_http_content_stream_copy_error", StringComparison.Ordinal):
                case WebException webEx
                        when webEx.Message.Contains("Canceled", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Socket closed", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Socket is closed", StringComparison.Ordinal) ||
                             webEx.Message.Contains("No address associated with hostname", StringComparison.Ordinal) ||
                             webEx.Message.Contains("Software caused connection abort", StringComparison.Ordinal):
                case IOException ioEx
                        when ioEx.Message.Contains("Network subsystem is down", StringComparison.Ordinal):
                    return false;
                default:
                    return true;
            }
        }

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

        public async Task<string> GetLog()
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
            if (ShouldLogException(ex) && _deviceInfo.DeviceType != DeviceType.Virtual)
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

#if !DEBUG
            return;
#endif

#pragma warning disable CS0162 // Unreachable code detected
            var logEntry = new List<string>
            {
                $"[{DateTime.UtcNow:o}]"
            };

            if (!string.IsNullOrEmpty(msg))
                logEntry.Add(msg);
            if (ex is not null)
                logEntry.Add(ex.ToString());
            if (data?.Any() == true)
                foreach (var d in data)
                    logEntry.Add($"{d.Key} : {d.Value}");

            lock (LogLock)
            {
                File.AppendAllLines(_logFilePath, logEntry);
            }
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}