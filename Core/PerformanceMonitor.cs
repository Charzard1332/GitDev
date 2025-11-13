using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace GitDev.Core
{
    /// <summary>
    /// Monitors system performance metrics including operation execution times and memory usage.
    /// </summary>
    public class PerformanceMonitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly List<OperationMetric> operationMetrics;
        private readonly int maxMetricsCount = 500;
        private readonly Process currentProcess;

        public PerformanceMonitor()
        {
            operationMetrics = new List<OperationMetric>();
            currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// Starts tracking an operation
        /// </summary>
        public OperationTracker StartOperation(string operationName)
        {
            return new OperationTracker(this, operationName);
        }

        /// <summary>
        /// Records a completed operation
        /// </summary>
        internal void RecordOperation(string operationName, long durationMs, bool success)
        {
            try
            {
                var metric = new OperationMetric
                {
                    OperationName = operationName,
                    DurationMs = durationMs,
                    Timestamp = DateTime.UtcNow,
                    Success = success
                };

                operationMetrics.Add(metric);

                // Keep only recent metrics
                if (operationMetrics.Count > maxMetricsCount)
                {
                    operationMetrics.RemoveAt(0);
                }

                logger.Debug($"Recorded operation: {operationName} ({durationMs}ms, Success: {success})");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error recording operation metric");
            }
        }

        /// <summary>
        /// Gets average operation duration for a specific operation type
        /// </summary>
        public double GetAverageOperationTime(string operationName)
        {
            var metrics = operationMetrics
                .Where(m => m.OperationName == operationName && m.Success)
                .ToList();

            return metrics.Any() ? metrics.Average(m => m.DurationMs) : 0;
        }

        /// <summary>
        /// Gets operation success rate
        /// </summary>
        public double GetSuccessRate(string operationName = null)
        {
            var metrics = operationName == null 
                ? operationMetrics 
                : operationMetrics.Where(m => m.OperationName == operationName).ToList();

            if (!metrics.Any()) return 100;

            return (double)metrics.Count(m => m.Success) / metrics.Count * 100;
        }

        /// <summary>
        /// Gets total operations count
        /// </summary>
        public int GetTotalOperationsCount()
        {
            return operationMetrics.Count;
        }

        /// <summary>
        /// Gets operations count by type
        /// </summary>
        public Dictionary<string, int> GetOperationsCounts()
        {
            return operationMetrics
                .GroupBy(m => m.OperationName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets current memory usage in MB
        /// </summary>
        public long GetCurrentMemoryUsageMB()
        {
            try
            {
                currentProcess.Refresh();
                return currentProcess.WorkingSet64 / (1024 * 1024);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets operations count for time period
        /// </summary>
        public int GetOperationsCountForPeriod(int minutes)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
            return operationMetrics.Count(m => m.Timestamp >= cutoffTime);
        }

        /// <summary>
        /// Gets slowest operations
        /// </summary>
        public List<OperationMetric> GetSlowestOperations(int count = 5)
        {
            return operationMetrics
                .OrderByDescending(m => m.DurationMs)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets failed operations
        /// </summary>
        public List<OperationMetric> GetFailedOperations(int count = 10)
        {
            return operationMetrics
                .Where(m => !m.Success)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public PerformanceStatistics GetStatistics()
        {
            return new PerformanceStatistics
            {
                TotalOperations = operationMetrics.Count,
                SuccessfulOperations = operationMetrics.Count(m => m.Success),
                FailedOperations = operationMetrics.Count(m => !m.Success),
                AverageDurationMs = operationMetrics.Any() ? operationMetrics.Average(m => m.DurationMs) : 0,
                MemoryUsageMB = GetCurrentMemoryUsageMB(),
                OperationsLast5Minutes = GetOperationsCountForPeriod(5),
                OperationsLast60Minutes = GetOperationsCountForPeriod(60)
            };
        }
    }

    /// <summary>
    /// Tracks a single operation's execution time
    /// </summary>
    public class OperationTracker : IDisposable
    {
        private readonly PerformanceMonitor monitor;
        private readonly string operationName;
        private readonly Stopwatch stopwatch;
        private bool success = true;

        internal OperationTracker(PerformanceMonitor monitor, string operationName)
        {
            this.monitor = monitor;
            this.operationName = operationName;
            this.stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Marks the operation as failed
        /// </summary>
        public void MarkAsFailed()
        {
            success = false;
        }

        public void Dispose()
        {
            stopwatch.Stop();
            monitor.RecordOperation(operationName, stopwatch.ElapsedMilliseconds, success);
        }
    }

    /// <summary>
    /// Represents a single operation metric
    /// </summary>
    public class OperationMetric
    {
        public string OperationName { get; set; }
        public long DurationMs { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Performance statistics summary
    /// </summary>
    public class PerformanceStatistics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public double AverageDurationMs { get; set; }
        public long MemoryUsageMB { get; set; }
        public int OperationsLast5Minutes { get; set; }
        public int OperationsLast60Minutes { get; set; }
    }
}
