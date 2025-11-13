using System;

namespace GitDev.Plugins
{
    /// <summary>
    /// Contains metadata information about a plugin.
    /// </summary>
    public class PluginMetadata
    {
        /// <summary>
        /// Gets or sets the unique identifier for the plugin.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the plugin name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the plugin version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the plugin author.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the plugin description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the minimum required GitDev version.
        /// </summary>
        public string MinimumHostVersion { get; set; }

        /// <summary>
        /// Gets or sets the plugin dependencies.
        /// </summary>
        public string[] Dependencies { get; set; }

        public PluginMetadata()
        {
            Dependencies = Array.Empty<string>();
        }

        public override string ToString()
        {
            return $"{Name} v{Version} by {Author}";
        }
    }
}
