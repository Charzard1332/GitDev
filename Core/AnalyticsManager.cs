using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using NLog;
using System.Web.Script.Serialization;

namespace GitDev.Core
{
    /// <summary>
    /// Manages analytics data including user activity, signups, and retention metrics.
    /// </summary>
    public class AnalyticsManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string dataFilePath;
        private AnalyticsData data;

        public AnalyticsManager(string dataDirectory = "data")
        {
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            
            dataFilePath = Path.Combine(dataDirectory, "analytics.json");
            LoadData();
        }

        /// <summary>
        /// Records a user activity event
        /// </summary>
        public void RecordActivity(string username, string activityType, string details = "")
        {
            try
            {
                var activity = new ActivityRecord
                {
                    Username = username,
                    ActivityType = activityType,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                data.Activities.Add(activity);
                
                // Keep only last 1000 activities
                if (data.Activities.Count > 1000)
                {
                    data.Activities.RemoveAt(0);
                }
                
                SaveData();
                logger.Debug($"Recorded activity: {activityType} for {username}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error recording activity");
            }
        }

        /// <summary>
        /// Records a user signup
        /// </summary>
        public void RecordSignup(string username)
        {
            try
            {
                if (!data.UserSignups.ContainsKey(username))
                {
                    data.UserSignups[username] = DateTime.UtcNow;
                    SaveData();
                    logger.Info($"Recorded signup for user: {username}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error recording signup");
            }
        }

        /// <summary>
        /// Records user last active timestamp
        /// </summary>
        public void RecordUserActive(string username)
        {
            try
            {
                data.LastActiveUsers[username] = DateTime.UtcNow;
                SaveData();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error recording user active status");
            }
        }

        /// <summary>
        /// Gets daily activity count
        /// </summary>
        public int GetDailyActivityCount()
        {
            var today = DateTime.UtcNow.Date;
            return data.Activities.Count(a => a.Timestamp.Date == today);
        }

        /// <summary>
        /// Gets weekly activity count
        /// </summary>
        public int GetWeeklyActivityCount()
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            return data.Activities.Count(a => a.Timestamp >= weekAgo);
        }

        /// <summary>
        /// Gets activity breakdown by type for a period
        /// </summary>
        public Dictionary<string, int> GetActivityBreakdown(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return data.Activities
                .Where(a => a.Timestamp >= cutoffDate)
                .GroupBy(a => a.ActivityType)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets total user signups count
        /// </summary>
        public int GetTotalSignups()
        {
            return data.UserSignups.Count;
        }

        /// <summary>
        /// Gets signups for a period
        /// </summary>
        public int GetSignupsForPeriod(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return data.UserSignups.Count(s => s.Value >= cutoffDate);
        }

        /// <summary>
        /// Gets retention rate (users active in last 7 days / total signups)
        /// </summary>
        public double GetRetentionRate()
        {
            if (data.UserSignups.Count == 0) return 0;
            
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var activeUsers = data.LastActiveUsers.Count(u => u.Value >= weekAgo);
            
            return (double)activeUsers / data.UserSignups.Count * 100;
        }

        /// <summary>
        /// Gets active users count for a period
        /// </summary>
        public int GetActiveUsersCount(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return data.LastActiveUsers.Count(u => u.Value >= cutoffDate);
        }

        /// <summary>
        /// Gets hourly activity distribution for the last 24 hours
        /// </summary>
        public Dictionary<int, int> GetHourlyActivityDistribution()
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var hourlyData = new Dictionary<int, int>();
            
            for (int i = 0; i < 24; i++)
            {
                hourlyData[i] = 0;
            }
            
            foreach (var activity in data.Activities.Where(a => a.Timestamp >= last24Hours))
            {
                int hour = activity.Timestamp.Hour;
                hourlyData[hour]++;
            }
            
            return hourlyData;
        }

        /// <summary>
        /// Gets recent activities
        /// </summary>
        public List<ActivityRecord> GetRecentActivities(int count = 10)
        {
            return data.Activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    var serializer = new JavaScriptSerializer();
                    data = serializer.Deserialize<AnalyticsData>(json);
                    
                    if (data == null)
                    {
                        data = new AnalyticsData();
                    }
                }
                else
                {
                    data = new AnalyticsData();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading analytics data, starting with fresh data");
                data = new AnalyticsData();
            }
        }

        private void SaveData()
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(data);
                File.WriteAllText(dataFilePath, json);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error saving analytics data");
            }
        }
    }

    /// <summary>
    /// Represents the analytics data structure
    /// </summary>
    public class AnalyticsData
    {
        public List<ActivityRecord> Activities { get; set; }
        public Dictionary<string, DateTime> UserSignups { get; set; }
        public Dictionary<string, DateTime> LastActiveUsers { get; set; }

        public AnalyticsData()
        {
            Activities = new List<ActivityRecord>();
            UserSignups = new Dictionary<string, DateTime>();
            LastActiveUsers = new Dictionary<string, DateTime>();
        }
    }

    /// <summary>
    /// Represents a single activity record
    /// </summary>
    public class ActivityRecord
    {
        public string Username { get; set; }
        public string ActivityType { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
