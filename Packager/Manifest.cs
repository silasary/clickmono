using ClickMono.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Packager
{
    public class Manifest
    {
        internal string iconFile = null;
        internal ManifestFile entryPoint;

        public string Version { get; set; } = string.Empty;

        public List<ManifestFile> Files { get; set; }

        public DeploymentOptions Deployment { get; } = new DeploymentOptions();
    }  
}
