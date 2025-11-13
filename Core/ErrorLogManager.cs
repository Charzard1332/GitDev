using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Targets;

namespace GitDev.Core
{
    /// <summary>
    /// Manages and aggregates error logs for display in the dashboard.
    /// </summary>
    public class ErrorLogManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly List<ErrorLogEntry> errorLogs;
        private readonly int maxErrorsCount = 200;

        public ErrorLogManager()
        {
            errorLogs = new List<ErrorLogEntry>();
            SetupLoggingTarget();
        }

        /// <summary>
        /// Sets up a custom NLog target to capture errors
        /// </summary>
        private void SetupLoggingTarget()
        {
            try
            {
                var config = LogManager.Configuration;
                var target = new MemoryTarget("ErrorCapture")
                {
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
                };

                config.AddTarget(target);
                config.LoggingRules.Add(new NLog.Config.LoggingRule("*", LogLevel.Error, target));
                LogManager.Configuration = config;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error setting up logging target");
            }
        }

        /// <summary>
        /// Records an error
        /// </summary>
        public void RecordError(string source, string message, string exceptionDetails = null)
        {
            try
            {
                var error = new ErrorLogEntry
                {
                    Source = source,
                    Message = message,
                    ExceptionDetails = exceptionDetails,
                    Timestamp = DateTime.UtcNow,
                    Severity = DetermineSeverity(message)
                };

                errorLogs.Add(error);

                // Keep only recent errors
                if (errorLogs.Count > maxErrorsCount)
                {
                    errorLogs.RemoveAt(0);
                }

                logger.Debug($"Recorded error from {source}: {message}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error recording error log");
            }
        }

        /// <summary>
        /// Gets error count for a period
        /// </summary>
        public int GetErrorCountForPeriod(int hours)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);
            return errorLogs.Count(e => e.Timestamp >= cutoffTime);
        }

        /// <summary>
        /// Gets errors by severity
        /// </summary>
        public Dictionary<ErrorSeverity, int> GetErrorsBySeverity()
        {
            return errorLogs
                .GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets errors by source
        /// </summary>
        public Dictionary<string, int> GetErrorsBySource()
        {
            return errorLogs
                .GroupBy(e => e.Source)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets recent errors
        /// </summary>
        public List<ErrorLogEntry> GetRecentErrors(int count = 10)
        {
            return errorLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets total error count
        /// </summary>
        public int GetTotalErrorCount()
        {
            return errorLogs.Count;
        }

        /// <summary>
        /// Gets errors for today
        /// </summary>
        public int GetTodayErrorCount()
        {
            var today = DateTime.UtcNow.Date;
            return errorLogs.Count(e => e.Timestamp.Date == today);
        }

        /// <summary>
        /// Gets hourly error distribution for the last 24 hours
        /// </summary>
        public Dictionary<int, int> GetHourlyErrorDistribution()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var hourlyData = new Dictionary<int, int>();

            for (int i = 0; i < 24; i++)
            {
                hourlyData[i] = 0;
            }

            foreach (var error in errorLogs.Where(e => e.Timestamp >= last24Hours))
            {
                int hour = error.Timestamp.Hour;
                hourlyData[hour]++;
            }

            return hourlyData;
        }

        /// <summary>
        /// Determines error severity based on message content
        /// </summary>
        private ErrorSeverity DetermineSeverity(string message)
        {
            if (message == null) return ErrorSeverity.Low;

            message = message.ToLower();

            if (message.Contains("fatal") || message.Contains("critical"))
                return ErrorSeverity.Critical;
            
            if (message.Contains("warning") || message.Contains("warn"))
                return ErrorSeverity.Medium;

            return ErrorSeverity.High;
        }
    }

    /// <summary>
    /// Represents a single error log entry
    /// </summary>
    public class ErrorLogEntry
    {
        public string Source { get; set; }
        public string Message { get; set; }
        public string ExceptionDetails { get; set; }
        public DateTime Timestamp { get; set; }
        public ErrorSeverity Severity { get; set; }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
