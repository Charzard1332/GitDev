using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace GitDev.Core
{
    /// <summary>
    /// Renders the analytics dashboard in the console with visualizations.
    /// </summary>
    public class DashboardRenderer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly AnalyticsManager analyticsManager;
        private readonly PerformanceMonitor performanceMonitor;
        private readonly ErrorLogManager errorLogManager;
        private readonly InteractiveCLI cli;

        public DashboardRenderer(
            AnalyticsManager analyticsManager,
            PerformanceMonitor performanceMonitor,
            ErrorLogManager errorLogManager,
            InteractiveCLI cli)
        {
            this.analyticsManager = analyticsManager;
            this.performanceMonitor = performanceMonitor;
            this.errorLogManager = errorLogManager;
            this.cli = cli;
        }

        /// <summary>
        /// Renders the complete dashboard
        /// </summary>
        public void RenderDashboard()
        {
            try
            {
                Console.Clear();
                
                // Header
                RenderHeader();
                
                // Overview section
                RenderOverview();
                
                // User Analytics section
                RenderUserAnalytics();
                
                // Performance section
                RenderPerformanceMetrics();
                
                // Error Logs section
                RenderErrorLogs();
                
                // Footer
                RenderFooter();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error rendering dashboard");
                cli.DisplayError("Error displaying dashboard");
            }
        }

        private void RenderHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         GitDev Analytics Dashboard                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void RenderOverview()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══ OVERVIEW ═══");
            Console.ResetColor();
            
            var stats = performanceMonitor.GetStatistics();
            
            Console.WriteLine($"  Total Operations:     {stats.TotalOperations}");
            Console.WriteLine($"  Memory Usage:         {stats.MemoryUsageMB} MB");
            Console.WriteLine($"  System Uptime:        {GetUptime()}");
            Console.WriteLine($"  Last Updated:         {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        private void RenderUserAnalytics()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══ USER ANALYTICS ═══");
            Console.ResetColor();

            int totalSignups = analyticsManager.GetTotalSignups();
            int activeUsers = analyticsManager.GetActiveUsersCount(7);
            double retentionRate = analyticsManager.GetRetentionRate();
            int dailyActivity = analyticsManager.GetDailyActivityCount();
            int weeklyActivity = analyticsManager.GetWeeklyActivityCount();

            Console.WriteLine($"  Total Signups:         {totalSignups}");
            Console.WriteLine($"  Active Users (7d):     {activeUsers}");
            Console.WriteLine($"  Retention Rate:        {retentionRate:F1}%");
            Console.WriteLine($"  Daily Activity:        {dailyActivity} actions");
            Console.WriteLine($"  Weekly Activity:       {weeklyActivity} actions");
            Console.WriteLine();

            // Activity breakdown
            var activityBreakdown = analyticsManager.GetActivityBreakdown(7);
            if (activityBreakdown.Any())
            {
                Console.WriteLine("  Activity Breakdown (Last 7 days):");
                foreach (var activity in activityBreakdown.OrderByDescending(a => a.Value).Take(5))
                {
                    Console.WriteLine($"    • {activity.Key,-20} {activity.Value,5} actions");
                }
                Console.WriteLine();
            }

            // Hourly activity chart
            RenderHourlyActivityChart();
        }

        private void RenderPerformanceMetrics()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("═══ SYSTEM PERFORMANCE ═══");
            Console.ResetColor();

            var stats = performanceMonitor.GetStatistics();
            double successRate = performanceMonitor.GetSuccessRate();

            Console.WriteLine($"  Operations (Total):     {stats.TotalOperations}");
            Console.WriteLine($"  Operations (Last 5m):   {stats.OperationsLast5Minutes}");
            Console.WriteLine($"  Operations (Last 1h):   {stats.OperationsLast60Minutes}");
            Console.WriteLine($"  Success Rate:           {successRate:F1}%");
            Console.WriteLine($"  Avg Duration:           {stats.AverageDurationMs:F2}ms");
            Console.WriteLine();

            // Operations by type
            var operationCounts = performanceMonitor.GetOperationsCounts();
            if (operationCounts.Any())
            {
                Console.WriteLine("  Top Operations:");
                foreach (var op in operationCounts.OrderByDescending(o => o.Value).Take(5))
                {
                    Console.WriteLine($"    • {op.Key,-20} {op.Value,5} calls");
                }
                Console.WriteLine();
            }

            // Slowest operations
            var slowest = performanceMonitor.GetSlowestOperations(3);
            if (slowest.Any())
            {
                Console.WriteLine("  Slowest Operations:");
                foreach (var op in slowest)
                {
                    Console.WriteLine($"    • {op.OperationName,-20} {op.DurationMs,6}ms");
                }
                Console.WriteLine();
            }
        }

        private void RenderErrorLogs()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("═══ ERROR LOGS ═══");
            Console.ResetColor();

            int totalErrors = errorLogManager.GetTotalErrorCount();
            int todayErrors = errorLogManager.GetTodayErrorCount();
            int last24hErrors = errorLogManager.GetErrorCountForPeriod(24);

            Console.WriteLine($"  Total Errors:          {totalErrors}");
            Console.WriteLine($"  Errors Today:          {todayErrors}");
            Console.WriteLine($"  Errors (24h):          {last24hErrors}");
            Console.WriteLine();

            // Errors by severity
            var errorsBySeverity = errorLogManager.GetErrorsBySeverity();
            if (errorsBySeverity.Any())
            {
                Console.WriteLine("  Errors by Severity:");
                foreach (var severity in errorsBySeverity.OrderByDescending(e => e.Value))
                {
                    ConsoleColor color = GetSeverityColor(severity.Key);
                    Console.ForegroundColor = color;
                    Console.WriteLine($"    • {severity.Key,-10} {severity.Value,5}");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            // Recent errors
            var recentErrors = errorLogManager.GetRecentErrors(5);
            if (recentErrors.Any())
            {
                Console.WriteLine("  Recent Errors:");
                foreach (var error in recentErrors)
                {
                    ConsoleColor color = GetSeverityColor(error.Severity);
                    Console.ForegroundColor = color;
                    Console.WriteLine($"    • [{error.Timestamp:HH:mm:ss}] {error.Source}: {TruncateString(error.Message, 50)}");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            // Hourly error chart
            RenderHourlyErrorChart();
        }

        private void RenderHourlyActivityChart()
        {
            var hourlyData = analyticsManager.GetHourlyActivityDistribution();
            Console.WriteLine("  Activity (Last 24h):");
            RenderBarChart(hourlyData, 30);
        }

        private void RenderHourlyErrorChart()
        {
            var hourlyData = errorLogManager.GetHourlyErrorDistribution();
            Console.WriteLine("  Errors (Last 24h):");
            RenderBarChart(hourlyData, 30);
        }

        private void RenderBarChart(Dictionary<int, int> data, int maxWidth)
        {
            if (!data.Any() || data.Values.All(v => v == 0))
            {
                Console.WriteLine("    No data available");
                Console.WriteLine();
                return;
            }

            int maxValue = data.Values.Max();
            if (maxValue == 0) maxValue = 1;

            // Show only relevant hours (current hour and previous hours)
            int currentHour = DateTime.UtcNow.Hour;
            var relevantHours = new List<int>();
            for (int i = 0; i < 12; i++)
            {
                relevantHours.Add((currentHour - 11 + i + 24) % 24);
            }

            foreach (var hour in relevantHours)
            {
                int value = data.ContainsKey(hour) ? data[hour] : 0;
                int barLength = (int)((double)value / maxValue * maxWidth);
                string bar = new string('█', barLength);
                
                Console.Write($"    {hour:D2}:00 ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(bar);
                Console.ResetColor();
                Console.WriteLine($" {value}");
            }
            Console.WriteLine();
        }

        private void RenderFooter()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Commands: 'dashboard' to refresh | 'exit' to quit                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private ConsoleColor GetSeverityColor(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Critical:
                    return ConsoleColor.Red;
                case ErrorSeverity.High:
                    return ConsoleColor.Yellow;
                case ErrorSeverity.Medium:
                    return ConsoleColor.DarkYellow;
                default:
                    return ConsoleColor.Gray;
            }
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength - 3) + "...";
        }

        private string GetUptime()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            if (uptime.TotalHours >= 1)
                return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
            else if (uptime.TotalMinutes >= 1)
                return $"{(int)uptime.TotalMinutes}m {uptime.Seconds}s";
            else
                return $"{uptime.Seconds}s";
        }
    }
}
