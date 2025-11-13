using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace GitDev.Plugins
{
    /// <summary>
    /// Manages the plugin system lifecycle and provides high-level plugin operations.
    /// </summary>
    public class PluginManager
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly PluginLoader _loader;
        private readonly IPluginContext _context;
        private static PluginManager _instance;
        private static readonly object _lock = new object();

        private PluginManager(string pluginDirectory, string hostVersion = "1.0.0")
        {
            var loggerForContext = LogManager.GetLogger("PluginContext");
            _context = new PluginContext(hostVersion, pluginDirectory, loggerForContext);
            _loader = new PluginLoader(pluginDirectory, _context);
        }

        /// <summary>
        /// Gets the singleton instance of PluginManager.
        /// </summary>
        public static PluginManager GetInstance(string pluginDirectory = null, string hostVersion = "1.0.0")
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        if (string.IsNullOrEmpty(pluginDirectory))
                        {
                            pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
                        }
                        _instance = new PluginManager(pluginDirectory, hostVersion);
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Initializes the plugin system and loads all plugins.
        /// </summary>
        public void Initialize()
        {
            logger.Info("Initializing plugin system...");
            _loader.LoadAllPlugins();
            logger.Info($"Plugin system initialized with {_loader.LoadedPlugins.Count} plugins.");
        }

        /// <summary>
        /// Executes a plugin by ID.
        /// </summary>
        public PluginResult ExecutePlugin(string pluginId, Dictionary<string, object> args = null)
        {
            try
            {
                var plugin = _loader.GetPlugin(pluginId);
                if (plugin == null)
                {
                    return PluginResult.Failed($"Plugin not found: {pluginId}");
                }

                logger.Info($"Executing plugin: {plugin.Metadata.Name}");
                var result = plugin.Execute(args ?? new Dictionary<string, object>());
                logger.Info($"Plugin execution completed: {plugin.Metadata.Name} - Success: {result.Success}");
                
                return result;
            }
            catch (Exception ex)
            {
                logger.Error($"Error executing plugin {pluginId}: {ex.Message}");
                return PluginResult.Failed($"Plugin execution failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lists all loaded plugins.
        /// </summary>
        public List<PluginMetadata> ListPlugins()
        {
            return _loader.ListPlugins();
        }

        /// <summary>
        /// Gets a plugin by ID.
        /// </summary>
        public IPlugin GetPlugin(string pluginId)
        {
            return _loader.GetPlugin(pluginId);
        }

        /// <summary>
        /// Loads a specific plugin from a file path.
        /// </summary>
        public bool LoadPlugin(string pluginPath)
        {
            return _loader.LoadPlugin(pluginPath);
        }

        /// <summary>
        /// Unloads a plugin by ID.
        /// </summary>
        public bool UnloadPlugin(string pluginId)
        {
            return _loader.UnloadPlugin(pluginId);
        }

        /// <summary>
        /// Shuts down the plugin system and unloads all plugins.
        /// </summary>
        public void Shutdown()
        {
            logger.Info("Shutting down plugin system...");
            _loader.UnloadAllPlugins();
            logger.Info("Plugin system shutdown complete.");
        }
    }
}
