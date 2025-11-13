using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace GitDev.Plugins
{
    /// <summary>
    /// Loads and manages plugins dynamically at runtime.
    /// </summary>
    public class PluginLoader
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly string _pluginDirectory;
        private readonly IPluginContext _context;
        private readonly Dictionary<string, IPlugin> _loadedPlugins;
        private readonly PluginSecurity _security;

        public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => _loadedPlugins;

        public PluginLoader(string pluginDirectory, IPluginContext context)
        {
            _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loadedPlugins = new Dictionary<string, IPlugin>();
            _security = new PluginSecurity();

            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                logger.Info($"Created plugin directory: {_pluginDirectory}");
            }
        }

        /// <summary>
        /// Discovers and loads all plugins from the plugin directory.
        /// </summary>
        public void LoadAllPlugins()
        {
            try
            {
                logger.Info("Starting plugin discovery...");
                var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);
                
                foreach (var pluginFile in pluginFiles)
                {
                    try
                    {
                        LoadPlugin(pluginFile);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to load plugin from {pluginFile}: {ex.Message}");
                    }
                }

                logger.Info($"Plugin discovery complete. Loaded {_loadedPlugins.Count} plugins.");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during plugin discovery: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a specific plugin from a file path.
        /// </summary>
        public bool LoadPlugin(string pluginPath)
        {
            if (!File.Exists(pluginPath))
            {
                logger.Error($"Plugin file not found: {pluginPath}");
                return false;
            }

            try
            {
                // Security validation
                if (!_security.ValidatePlugin(pluginPath))
                {
                    logger.Warn($"Plugin failed security validation: {pluginPath}");
                    return false;
                }

                // Load the assembly
                var assembly = Assembly.LoadFrom(pluginPath);
                
                // Find types that implement IPlugin
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (pluginTypes.Count == 0)
                {
                    logger.Warn($"No IPlugin implementations found in {pluginPath}");
                    return false;
                }

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType);
                        
                        // Validate metadata
                        if (plugin.Metadata == null || string.IsNullOrEmpty(plugin.Metadata.Id))
                        {
                            logger.Error($"Plugin {pluginType.Name} has invalid metadata");
                            continue;
                        }

                        // Check if plugin is already loaded
                        if (_loadedPlugins.ContainsKey(plugin.Metadata.Id))
                        {
                            logger.Warn($"Plugin {plugin.Metadata.Id} is already loaded");
                            continue;
                        }

                        // Initialize the plugin
                        plugin.Initialize(_context);
                        
                        // Add to loaded plugins
                        _loadedPlugins[plugin.Metadata.Id] = plugin;
                        logger.Info($"Successfully loaded plugin: {plugin.Metadata}");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to instantiate plugin {pluginType.Name}: {ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading plugin from {pluginPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unloads a specific plugin by ID.
        /// </summary>
        public bool UnloadPlugin(string pluginId)
        {
            if (!_loadedPlugins.ContainsKey(pluginId))
            {
                logger.Warn($"Plugin {pluginId} is not loaded");
                return false;
            }

            try
            {
                var plugin = _loadedPlugins[pluginId];
                plugin.Cleanup();
                _loadedPlugins.Remove(pluginId);
                logger.Info($"Successfully unloaded plugin: {pluginId}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Error unloading plugin {pluginId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unloads all loaded plugins.
        /// </summary>
        public void UnloadAllPlugins()
        {
            logger.Info("Unloading all plugins...");
            var pluginIds = _loadedPlugins.Keys.ToList();
            
            foreach (var pluginId in pluginIds)
            {
                UnloadPlugin(pluginId);
            }
            
            logger.Info("All plugins unloaded.");
        }

        /// <summary>
        /// Gets a loaded plugin by ID.
        /// </summary>
        public IPlugin GetPlugin(string pluginId)
        {
            return _loadedPlugins.ContainsKey(pluginId) ? _loadedPlugins[pluginId] : null;
        }

        /// <summary>
        /// Lists all loaded plugins.
        /// </summary>
        public List<PluginMetadata> ListPlugins()
        {
            return _loadedPlugins.Values.Select(p => p.Metadata).ToList();
        }
    }
}
