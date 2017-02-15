using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Packager
{
    public class Manifest
    {
        internal string iconFile = null;
        internal ManifestFile entryPoint;

        public string Version { get; set; } = string.Empty;

        public List<ManifestFile> Files { get; set; }
        public string DeploymentProviderUrl { get; internal set; } = string.Empty;
    }

    public class ManifestFile
    {
        public ManifestFile(FileInfo file)
        {
            Name = file.Name;
            try
            {
                var asm = Assembly.LoadFile(file.FullName);
                AssemblyName = asm.GetName().Name;
                Version = asm.GetName().Version.ToString();
                PublicKeyToken = BitConverter.ToString(asm.GetName().GetPublicKeyToken()).ToUpperInvariant().Replace("-", "");
                Product = asm.GetCustomAttributes<AssemblyProductAttribute>().SingleOrDefault()?.Product;
                Publisher = asm.GetCustomAttributes<AssemblyCompanyAttribute>().SingleOrDefault()?.Company;
                Architecture = asm.GetName().ProcessorArchitecture;
            }
            catch (BadImageFormatException) // It's not a dotnot assembly.
            {
                AssemblyName = null;
                Version = null;
                PublicKeyToken = null;
                Architecture = ProcessorArchitecture.None;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Console.WriteLine($"Failed. {e.Message}");
            }
            if (string.IsNullOrWhiteSpace(PublicKeyToken))
                PublicKeyToken = null;
            DigestMethod = "sha256";
            DigestValue = Crypto.GetSha256DigestValue(file);
            Size = file.Length;
        }

        public string Name { get; set; }

        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string PublicKeyToken { get; set; }

        public string DigestMethod { get; set; }

        public string DigestValue { get; set; }

        public string Product { get; set; }

        public string Publisher { get; set; }

        public long Size { get; set; }
        public ProcessorArchitecture Architecture { get; private set; }
    }
}
