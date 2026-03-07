using System;
using System.Diagnostics;
using System.IO;
namespace Optimizer.Helpers
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "optimizer_debug.log");
        private static readonly object _lock = new object();
        private static bool _isLogging = false;
        // Active ou désactive les logs de debug (false en production)
        private static readonly bool ENABLE_LOGGING = false;
        static Logger()
        {
            if (ENABLE_LOGGING)
            {
                try
                {
                    File.WriteAllText(LogFilePath, $"=== Optimizer Debug Log - {DateTime.Now} ==={Environment.NewLine}");
                }
                catch { }
            }
        }
        public static void Log(string message)
        {
            if (!ENABLE_LOGGING) return;
            if (_isLogging) return;
            lock (_lock)
            {
                _isLogging = true;
                try
                {
                    string logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                    Debug.WriteLine(logMessage);
                }
                catch { }
                finally
                {
                    _isLogging = false;
                }
            }
        }
    }
}