using System;
using System.Collections.Generic;

namespace GitDev.Plugins
{
    /// <summary>
    /// Base interface that all plugins must implement.
    /// Defines the contract for plugin lifecycle and interaction with the GitDev system.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the metadata information for this plugin.
        /// </summary>
        PluginMetadata Metadata { get; }

        /// <summary>
        /// Called when the plugin is loaded and should initialize itself.
        /// </summary>
        /// <param name="context">The plugin context containing host application information</param>
        void Initialize(IPluginContext context);

        /// <summary>
        /// Executes the plugin's main functionality.
        /// </summary>
        /// <param name="args">Arguments passed to the plugin</param>
        /// <returns>Execution result containing status and output</returns>
        PluginResult Execute(Dictionary<string, object> args);

        /// <summary>
        /// Called when the plugin is being unloaded. Perform cleanup here.
        /// </summary>
        void Cleanup();
    }
}
