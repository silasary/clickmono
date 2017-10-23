using System;
using System.Collections.Generic;

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
        public List<string> Includes { get; internal set; } = new List<string>();
        public List<string> Excludes { get; internal set; } = new List<string>();
    }
}