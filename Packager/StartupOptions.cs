using System;
namespace Packager
{
    internal class StartupOptions
    {
        public enum Modes
        {
            Generate,
            Update
        }

        public Modes Mode { get; internal set; }
        public string DeploymentProvider { get; internal set; }
        public string Target { get; internal set; }
        public string GenerateBootstrap { get; internal set; }
    }
}