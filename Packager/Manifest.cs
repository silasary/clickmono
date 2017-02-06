using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Packager
{
    public class Manifest
    {
        internal string iconFile;
        internal ManifestFile entryPoint;

        public string version { get; set; } = string.Empty;

        public List<ManifestFile> files { get; set; }
        public string DeploymentProviderUrl { get; internal set; } = string.Empty;
    }

    public class ManifestFile
    {
        public string Name { get; set; }

        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string PublicKeyToken { get; set; }

        public string DigestMethod { get; set; }

        public string DigestValue { get; set; }

        public string Product { get; set; }

        public string Publisher { get; set; }

        public long Size { get; set; }
    }
}
