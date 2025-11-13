using System;
using System.Collections.Generic;
using NLog;

namespace GitDev.Plugins
{
    /// <summary>
    /// Default implementation of IPluginContext.
    /// </summary>
    public class PluginContext : IPluginContext
    {
        private readonly Dictionary<string, string> _config;
        private readonly ILogger _logger;

        public string HostVersion { get; }
        public string PluginDirectory { get; }
        public ILogger Logger => _logger;

        public PluginContext(string hostVersion, string pluginDirectory, ILogger logger)
        {
            HostVersion = hostVersion ?? "1.0.0";
            PluginDirectory = pluginDirectory;
            _logger = logger;
            _config = new Dictionary<string, string>();
        }

        public string GetConfigValue(string key)
        {
            return _config.ContainsKey(key) ? _config[key] : null;
        }

        public void SetConfigValue(string key, string value)
        {
            _config[key] = value;
        }
    }
}
