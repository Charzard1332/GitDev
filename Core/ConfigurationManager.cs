using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using NLog;

namespace GitDev.Core
{
    /// <summary>
    /// Manages application configuration with support for persistence.
    /// </summary>
    public class ConfigurationManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string configFilePath;
        private Dictionary<string, object> configuration;

        public ConfigurationManager(string configFileName = "gitdev.config.json")
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitDev");
            Directory.CreateDirectory(appDataPath);
            
            configFilePath = Path.Combine(appDataPath, configFileName);
            configuration = new Dictionary<string, object>();
            
            LoadConfiguration();
        }

        /// <summary>
        /// Load configuration from file.
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    logger.Debug($"Loading configuration from {configFilePath}");
                    var json = File.ReadAllText(configFilePath);
                    var serializer = new JavaScriptSerializer();
                    configuration = serializer.Deserialize<Dictionary<string, object>>(json) 
                                  ?? new Dictionary<string, object>();
                    logger.Info("Configuration loaded successfully");
                }
                else
                {
                    logger.Info("No existing configuration file found, using defaults");
                    SetDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load configuration");
                SetDefaultConfiguration();
            }
        }

        /// <summary>
        /// Save configuration to file.
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                logger.Debug($"Saving configuration to {configFilePath}");
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(configuration);
                File.WriteAllText(configFilePath, json);
                logger.Info("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to save configuration");
            }
        }

        /// <summary>
        /// Set default configuration values.
        /// </summary>
        private void SetDefaultConfiguration()
        {
            configuration["MaxConcurrentOperations"] = 3;
            configuration["WebSocketPort"] = 5001;
            configuration["CommandHistorySize"] = 50;
            configuration["EnableFileWatcher"] = true;
            configuration["LogLevel"] = "Info";
            configuration["DefaultRepoPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GitDev");
            
            SaveConfiguration();
        }

        /// <summary>
        /// Get a configuration value.
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                if (configuration.ContainsKey(key))
                {
                    var value = configuration[key];
                    
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }
                    
                    // Try to convert
                    if (value != null)
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Failed to get configuration value for key: {key}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Set a configuration value.
        /// </summary>
        public void SetValue(string key, object value)
        {
            try
            {
                configuration[key] = value;
                logger.Debug($"Configuration value set: {key} = {value}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to set configuration value for key: {key}");
            }
        }

        /// <summary>
        /// Check if a configuration key exists.
        /// </summary>
        public bool HasKey(string key)
        {
            return configuration.ContainsKey(key);
        }

        /// <summary>
        /// Remove a configuration value.
        /// </summary>
        public void RemoveValue(string key)
        {
            try
            {
                if (configuration.ContainsKey(key))
                {
                    configuration.Remove(key);
                    logger.Debug($"Configuration value removed: {key}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to remove configuration value for key: {key}");
            }
        }

        /// <summary>
        /// Get all configuration keys.
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return configuration.Keys;
        }

        /// <summary>
        /// Display all configuration values.
        /// </summary>
        public void DisplayConfiguration()
        {
            Console.WriteLine("\n═══════════════════ CONFIGURATION ═══════════════════");
            foreach (var kvp in configuration)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine("════════════════════════════════════════════════════");
        }
    }
}
