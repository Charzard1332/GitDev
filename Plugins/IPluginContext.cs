using System;
using NLog;

namespace GitDev.Plugins
{
    /// <summary>
    /// Provides context and access to the host application for plugins.
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Gets the host application version.
        /// </summary>
        string HostVersion { get; }

        /// <summary>
        /// Gets the plugin directory path.
        /// </summary>
        string PluginDirectory { get; }

        /// <summary>
        /// Gets the logger instance for the plugin.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Gets a configuration value for the plugin.
        /// </summary>
        string GetConfigValue(string key);

        /// <summary>
        /// Sets a configuration value for the plugin.
        /// </summary>
        void SetConfigValue(string key, string value);
    }
}
