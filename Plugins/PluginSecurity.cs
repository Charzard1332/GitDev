using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NLog;

namespace GitDev.Plugins
{
    /// <summary>
    /// Provides security validation for plugins.
    /// </summary>
    public class PluginSecurity
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly string[] _allowedExtensions = { ".dll" };
        private readonly long _maxPluginSize = 50 * 1024 * 1024; // 50 MB

        /// <summary>
        /// Validates a plugin file before loading.
        /// </summary>
        public bool ValidatePlugin(string pluginPath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(pluginPath))
                {
                    logger.Error($"Plugin file does not exist: {pluginPath}");
                    return false;
                }

                // Check file extension
                var extension = Path.GetExtension(pluginPath).ToLower();
                if (!_allowedExtensions.Contains(extension))
                {
                    logger.Error($"Invalid plugin file extension: {extension}");
                    return false;
                }

                // Check file size
                var fileInfo = new FileInfo(pluginPath);
                if (fileInfo.Length > _maxPluginSize)
                {
                    logger.Error($"Plugin file too large: {fileInfo.Length} bytes (max: {_maxPluginSize})");
                    return false;
                }

                // Additional security checks can be added here:
                // - Digital signature verification
                // - Hash whitelist checking
                // - Scan for malicious patterns

                logger.Info($"Plugin passed security validation: {pluginPath}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error validating plugin {pluginPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Computes the SHA256 hash of a plugin file.
        /// </summary>
        public string ComputePluginHash(string pluginPath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(pluginPath))
                    {
                        var hash = sha256.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error computing hash for {pluginPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a plugin is in the whitelist (if whitelist exists).
        /// </summary>
        public bool IsPluginWhitelisted(string pluginHash, string whitelistPath = null)
        {
            if (string.IsNullOrEmpty(whitelistPath) || !File.Exists(whitelistPath))
            {
                // If no whitelist, allow all plugins (for development)
                return true;
            }

            try
            {
                var whitelist = File.ReadAllLines(whitelistPath);
                return whitelist.Any(hash => hash.Trim().Equals(pluginHash, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking whitelist: {ex.Message}");
                return false;
            }
        }
    }
}
