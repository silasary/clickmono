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

        public string version { get; set; }

        public List<ManifestFile> files { get; set; }
    }

    public class ManifestFile
    {
        public string path { get; set; }

        public string name { get; set; }

        public string assemblyName { get; set; }

        public string version { get; set; }

        public string publicKeyToken { get; set; }

        public string digestMethod { get; set; }

        public string digestValue { get; set; }

        public long size { get; set; }
    }
}
