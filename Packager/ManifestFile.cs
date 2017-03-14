using ClickMono.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Packager
{
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

        public ManifestFile(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "file":
                    Name = element.Attribute("name").Value;
                    break;

                case "dependentAssembly":
                    //DependencyType = element.Attribute("dependencyType"); // TODO: enum { install, preRequisite}
                    //AllowDelayedBinding = bool.Parse(element.Attribute("allowDelayedBinding").Value);
                    var assemblyIdentity = element.Element(Xmlns.asmv1assemblyIdentity);
                    Name = assemblyIdentity.Attribute("name").Value;
                    Version = assemblyIdentity.Attribute("version").Value;
                    //Language = assemblyIdentity.Attribute("language").Value;
                    Architecture = (ProcessorArchitecture)Enum.Parse(typeof(ProcessorArchitecture), assemblyIdentity.Attribute("processorArchitecture").Value, true);
                    break;
                default:
                    throw new ArgumentException("Not valid XML");
            }

            var hash = element.Element(Xmlns.asmv2hash);
            DigestValue = hash.Element(Xmlns.dsigDigestValue).Value;
            DigestMethod = hash.Element(Xmlns.dsigDigestMethod).Attribute("Algorithm").Value.Split('#')[1];
            Size = long.Parse(element.Attribute("size").Value);
        }

        public string Name { get; set; }

        public string AssemblyName { get; set; }

        public string Version { get; set; }

        public string PublicKeyToken { get; set; }

        public string DigestMethod { get; set; }

        public string DigestValue { get; set; }


        public long Size { get; set; }

        public ProcessorArchitecture Architecture { get; private set; }

        /// <summary>
        /// Only relevant for Entry Point
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// Only relevant for Entry Point
        /// </summary>
        public string Publisher { get; set; }
    }
}
