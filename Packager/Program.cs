using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Packager
{
    class Program
    {
        private const string CONST_HASH_TRANSFORM_IDENTITY = "urn:schemas-microsoft-com:HashTransforms.Identity";
        private const string CONST_NULL_PUBKEY = "0000000000000000";

        static void Main(string[] args)
        {
            if (args.Length == 0 && Debugger.IsAttached)
            {
                args = new string[] { Assembly.GetExecutingAssembly().Location };
            }
            else if (args.Length == 0)
            {
                Console.WriteLine("No target specified.");
                return;
            }
            var project = args[0];
            var directory = new DirectoryInfo(Path.GetDirectoryName(project));
            var target = directory.CreateSubdirectory("_publish");

            var date = DateTime.UtcNow;
            var major = date.ToString("yyMM");
            var minor = date.ToString("ddHH");
            var patch = date.ToString("mmss");
            var build = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "0";

            var manifest = new Manifest();
            manifest.version = major + "." + minor + "." + patch + "." + build;
            EnumerateFiles(directory, manifest);
            manifest.entryPoint = manifest.files.Single(n => n.name == Path.GetFileName(project));
            var xml = GenerateManifest(directory, manifest);
            string manifestPath = Path.Combine(target.FullName, Path.GetFileName(project) + ".manifest");
            File.WriteAllText(manifestPath, xml.ToString(SaveOptions.OmitDuplicateNamespaces));

            foreach (var file in manifest.files)
            {
                File.Copy(Path.Combine(directory.FullName, file.name), Path.Combine(target.FullName, file.name), true);
            }
            xml = GenerateApplicationManifest(manifest, File.ReadAllBytes(manifestPath));
            File.WriteAllText(Path.Combine(target.FullName, Path.GetFileName(project) + ".application"), xml.ToString(SaveOptions.OmitDuplicateNamespaces));
        }

        private static void EnumerateFiles(DirectoryInfo directory, Manifest manifest)
        {
            manifest.files = new List<ManifestFile>();

            Stack<FileInfo> content = new Stack<FileInfo>();

            foreach (var file in directory.EnumerateFiles())
            {
                string version = null;
                string publicKeyToken = null;
                string assemblyName = null;
                string product = null;
                string publisher = null;

                if (file.Name.Contains(".vshost"))
                    continue;

                if (!(file.Extension == ".dll" || file.Extension == ".exe"))
                {
                    content.Push(file);
                    continue;
                }
                Console.WriteLine("Processing " + file.Name + "...");

                try
                {
                    var asm = Assembly.LoadFile(file.FullName);
                    assemblyName = asm.GetName().Name;
                    version = asm.GetName().Version.ToString();
                    publicKeyToken = BitConverter.ToString(asm.GetName().GetPublicKeyToken()).ToUpperInvariant().Replace("-", "");
                    product = asm.GetCustomAttributes<AssemblyProductAttribute>().SingleOrDefault()?.Product;
                    publisher = asm.GetCustomAttributes<AssemblyCompanyAttribute>().SingleOrDefault()?.Company;
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    Console.WriteLine($"Failed. {e.Message}");
                }

                manifest.files.Add(new ManifestFile
                {
                    path = /*"Application Files/" + manifest.version + "/" +*/ file.Name + ".deploy",
                    name = file.Name,
                    assemblyName = assemblyName,
                    version = version,
                    publicKeyToken = string.IsNullOrWhiteSpace(publicKeyToken) ? null : publicKeyToken,
                    digestMethod = "sha256",
                    digestValue = Crypto.GetSha256DigestValue(file),
                    size = file.Length,
                    Product = product,
                    Publisher = publisher,
                });

            }

            foreach (var file in content)
            {
                if (file.Name.Contains(".vshost"))
                    continue;
                Console.WriteLine($"Adding file {file.Name}");
                if (file.Extension == ".ico" && string.IsNullOrEmpty(manifest.iconFile))
                    manifest.iconFile = file.Name;
                manifest.files.Add(new ManifestFile
                {
                    path = "Application Files/" + manifest.version + "/" + file.Name,
                    name = file.Name,
                    assemblyName = null,
                    version = null,
                    publicKeyToken = null,
                    digestMethod = "sha256",
                    digestValue = Crypto.GetSha256DigestValue(file),
                    size = file.Length
                });
            }
        }

        private static XDocument GenerateManifest(DirectoryInfo directory, Manifest manifest)
        {
            var asmv1ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
            var asmv2ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v2");
            var asmv3ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v3");
            var dsigns = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");

            var asmv1assembly = asmv1ns.GetName("assembly");
            var asmv1assemblyIdentity = asmv1ns.GetName("assemblyIdentity");
            var asmv2application = asmv2ns.GetName("application");
            var asmv2entryPoint = asmv2ns.GetName("entryPoint");
            var asmv2assemblyIdentity = asmv2ns.GetName("assemblyIdentity");
            var asmv2trustInfo = asmv2ns.GetName("trustInfo");
            var asmv2security = asmv2ns.GetName("security");
            var asmv2applicationRequestMinimum = asmv2ns.GetName("applicationRequestMinimum");
            var asmv2PermissionSet = asmv2ns.GetName("PermissionSet");
            var asmv2defaultAssemblyRequest = asmv2ns.GetName("defaultAssemblyRequest");
            var asmv2dependency = asmv2ns.GetName("dependency");
            var asmv2dependentAssembly = asmv2ns.GetName("dependentAssembly");
            var asmv2hash = asmv2ns.GetName("hash");
            var asmv2file = asmv2ns.GetName("file");
            var asmv2commandLine = asmv2ns.GetName("commandLine");
            var asmv2dependentOS = asmv2ns.GetName("dependentOS");
            var asmv2osVersionInfo = asmv2ns.GetName("osVersionInfo");
            var asmv2os = asmv2ns.GetName("os");
            var asmv3requestedPrivileges = asmv3ns.GetName("requestedPrivileges");
            var asmv3requestedExecutionLevel = asmv3ns.GetName("requestedExecutionLevel");
            var dsigTransforms = dsigns.GetName("Transforms");
            var dsigTransform = dsigns.GetName("Transform");
            var dsigAlgorithm = XName.Get("Algorithm");// dsigns.GetName("Algorithm");
            var dsigDigestMethod = dsigns.GetName("DigestMethod");
            var dsigDigestValue = dsigns.GetName("DigestValue");

            var documentElements = new List<object>
            {
                new XAttribute(XNamespace.Xmlns + "asmv1", asmv1ns),
                new XAttribute("xmlns", asmv2ns),
                new XAttribute(XNamespace.Xmlns + "asmv3ns", asmv3ns),
                new XAttribute(XNamespace.Xmlns + "dsig", dsigns),
                new XAttribute("manifestVersion", "1.0"),
                GetManifestAssemblyIdentity(asmv1assemblyIdentity, manifest),
                new XElement(asmv2application),
                new XElement(asmv2entryPoint,
                    GetDependencyAssemblyIdentity(asmv2assemblyIdentity, manifest.entryPoint),
                    new XElement(asmv2commandLine,
                        new XAttribute("file", manifest.entryPoint.name),
                        new XAttribute("parameters", ""))
                ),
                new XElement(asmv2trustInfo,
                    new XElement(asmv2security,
                        new XElement(asmv2applicationRequestMinimum,
                            new XElement(asmv2PermissionSet,
                                new XAttribute("Unrestricted", "true"),
                                new XAttribute("ID", "Custom"),
                                new XAttribute("SameSite", "site")),
                            new XElement(asmv2defaultAssemblyRequest,
                                new XAttribute("permissionSetReference", "Custom"))),
                        new XElement(asmv3requestedPrivileges,
                            new XElement(asmv3requestedExecutionLevel,
                                new XAttribute("level", "asInvoker"),
                                new XAttribute("uiAccess", "false"))))
                ),
                // For reasons I don't quite understand, all clickonce manifests are marked compatible with XP+ (even those on Framework 4.6+)
                new XElement(asmv2dependency,
                    new XElement(asmv2dependentOS,
                        new XElement(asmv2osVersionInfo,
                            new XElement(asmv2os,
                                new XAttribute("majorVersion", "5"),
                                new XAttribute("minorVersion", "1"),
                                new XAttribute("buildNumber", "2600"),
                                new XAttribute("servicePackMajor", "0"))))
                ),
                new XElement(asmv2dependency,
                    new XElement(asmv2dependentAssembly,
                        new XAttribute("dependencyType", "preRequisite"),
                        new XAttribute("allowDelayedBinding", "true"),
                        new XElement(asmv2assemblyIdentity,
                            new XAttribute("name", "Microsoft.Windows.CommonLanguageRuntime"),
                            new XAttribute("version", "4.0.30319.0")))
                )
            };
            
            foreach (var item in manifest.files)
            {
                if (item.version != null)
                {
                    documentElements.Add(
                        new XElement(asmv2dependency,
                            new XElement(asmv2dependentAssembly,
                                new XAttribute("dependencyType", "install"),
                                new XAttribute("allowDelayedBinding", "true"),
                                new XAttribute("codebase", item.name.Replace("/", "\\")),
                                new XAttribute("size", item.size.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                GetDependencyAssemblyIdentity(asmv2assemblyIdentity, item),
                                new XElement(asmv2hash,
                                    new XElement(dsigTransforms,
                                        new XElement(dsigTransform,
                                            new XAttribute("Algorithm", CONST_HASH_TRANSFORM_IDENTITY))),
                                    new XElement(dsigDigestMethod,
                                        new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#" + item.digestMethod)),
                                    new XElement(dsigDigestValue,
                                        new XText(item.digestValue))))));
                }
                else
                {
                    documentElements.Add(
                        new XElement(asmv2file,
                            new XAttribute("name", item.name.Replace("/", "\\")),
                            new XAttribute("size", item.size.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                            new XElement(asmv2hash,
                                new XElement(dsigTransforms,
                                    new XElement(dsigTransform,
                                        new XAttribute("Algorithm", CONST_HASH_TRANSFORM_IDENTITY))),
                                new XElement(dsigDigestMethod,
                                    new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#" + item.digestMethod)),
                                new XElement(dsigDigestValue,
                                    new XText(item.digestValue)))));
                }
            }
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(asmv1assembly, documentElements));
        }

        private static XElement GetManifestAssemblyIdentity(XName asmv1assemblyIdentity, Manifest manifest)
        {
            return new XElement(asmv1assemblyIdentity,
                new XAttribute("name", manifest.entryPoint.name),
                new XAttribute("version", manifest.version),
                new XAttribute("publicKeyToken", CONST_NULL_PUBKEY),
                new XAttribute("language", "neutral"),
                new XAttribute("processorArchitecture", "msil"),
                new XAttribute("type", "win32")
            );
        }

        private static XElement GetDependencyAssemblyIdentity(XName asmv2assemblyIdentity, ManifestFile file)
        {
            var assemblyIdentityAttributes = new List<XAttribute>
            {
                new XAttribute("name", file.assemblyName ?? file.name.Substring(0, file.name.LastIndexOf("."))),
                new XAttribute("version", file.version),
                new XAttribute("language", "neutral"),
                new XAttribute("processorArchitecture", "msil"),
            };

            if (file.publicKeyToken != null)
            {
                assemblyIdentityAttributes.Add(
                    new XAttribute("publicKeyToken", file.publicKeyToken));
            }

            return new XElement(asmv2assemblyIdentity, assemblyIdentityAttributes);
        }

        public static XDocument GenerateApplicationManifest(Manifest manifest, byte[] manifestBytes)
        {
            var asmv1ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
            var asmv2ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v2");
            var clickoncev2ns = XNamespace.Get("urn:schemas-microsoft-com:clickonce.v2");
            var dsigns = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");

            var asmv1assembly = asmv1ns.GetName("assembly");
            var asmv1assemblyIdentity = asmv1ns.GetName("assemblyIdentity");
            var asmv1name = asmv1ns.GetName("name");
            var asmv1version = asmv1ns.GetName("version");
            var asmv1publicKeyToken = asmv1ns.GetName("publicKeyToken");
            var asmv1language = asmv1ns.GetName("language");
            var asmv1processorArchitecture = asmv1ns.GetName("processorArchitecture");
            var asmv1description = asmv1ns.GetName("description");
            var asmv2publisher = asmv2ns.GetName("publisher");
            var asmv2product = asmv2ns.GetName("product");
            var asmv2deployment = asmv2ns.GetName("deployment");
            var asmv2install = asmv2ns.GetName("install");
            var asmv2mapFileExtensions = asmv2ns.GetName("mapFileExtensions");
            var asmv2subscription = asmv2ns.GetName("subscription");
            var asmv2update = asmv2ns.GetName("update");
            var asmv2beforeApplicationStartup = asmv2ns.GetName("beforeApplicationStartup");
            var asmv2deploymentProvider = asmv2ns.GetName("deploymentProvider");
            var asmv2codebase = asmv2ns.GetName("codebase");
            var asmv2dependency = asmv2ns.GetName("dependency");
            var asmv2dependentAssembly = asmv2ns.GetName("dependentAssembly");
            var asmv2dependencyType = asmv2ns.GetName("dependencyType");
            var asmv2size = asmv2ns.GetName("size");
            var asmv2assemblyIdentity = asmv2ns.GetName("assemblyIdentity");
            var asmv2name = asmv2ns.GetName("name");
            var asmv2version = asmv2ns.GetName("version");
            var asmv2publicKeyToken = asmv2ns.GetName("publicKeyToken");
            var asmv2language = asmv2ns.GetName("language");
            var asmv2processorArchitecture = asmv2ns.GetName("processorArchitecture");
            var asmv2type = asmv2ns.GetName("type");
            var asmv2hash = asmv2ns.GetName("hash");
            var clickoncev2compatibleFrameworks = clickoncev2ns.GetName("compatibleFrameworks");
            var clickoncev2framework = clickoncev2ns.GetName("framework");
            var clickoncev2targetVersion = clickoncev2ns.GetName("targetVersion");
            var clickoncev2profile = clickoncev2ns.GetName("profile");
            var clickoncev2supportedRuntime = clickoncev2ns.GetName("supportedRuntime");
            var dsigTransforms = dsigns.GetName("Transforms");
            var dsigTransform = dsigns.GetName("Transform");
            var dsigAlgorithm = XName.Get("Algorithm");// dsigns.GetName("Algorithm");
            var dsigDigestMethod = dsigns.GetName("DigestMethod");
            var dsigDigestValue = dsigns.GetName("DigestValue");

            var manifestSize = manifestBytes.Length;
            var manifestDigest = Crypto.GetSha256DigestValue(manifestBytes);

            if (string.IsNullOrWhiteSpace(manifest.entryPoint.Publisher))
                manifest.entryPoint.Publisher = Environment.UserName;
            if (string.IsNullOrWhiteSpace(manifest.entryPoint.Product))
                manifest.entryPoint.Product = manifest.entryPoint.name;

             var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(asmv1assembly,
                    new XAttribute(XNamespace.Xmlns + "asmv1", asmv1ns),
                    new XAttribute(XNamespace.Xmlns + "asmv2", asmv2ns),
                    new XAttribute(XNamespace.Xmlns + "clickoncev2ns", clickoncev2ns),
                    new XAttribute(XNamespace.Xmlns + "dsig", dsigns),
                    new XAttribute("manifestVersion", "1.0"),
                    new XElement(asmv1assemblyIdentity,
                        new XAttribute("name", Path.ChangeExtension(manifest.entryPoint.name , ".application")),
                        new XAttribute("version", manifest.version),
                        new XAttribute("publicKeyToken", "0000000000000000"),
                        new XAttribute("language", "neutral"),
                        new XAttribute("processorArchitecture", "msil")
                    ),
                    new XElement(asmv1description,
                        new XAttribute(asmv2publisher, manifest.entryPoint.Publisher),
                        new XAttribute(asmv2product, manifest.entryPoint.Product)
                    ),
                    new XElement(asmv2deployment,
                        new XAttribute("install", "true"),
                        new XAttribute("mapFileExtensions", "true"),
                        new XAttribute("trustURLParameters", "true"),
                    new XElement(asmv2subscription,
                        new XElement(asmv2update,
                            new XElement(asmv2beforeApplicationStartup))),
                        new XElement(asmv2deploymentProvider,
                            new XAttribute("codebase", manifest.DeploymentProviderUrl))
                    ),
                    new XElement(clickoncev2compatibleFrameworks,
                        new XElement(clickoncev2framework,
                            new XAttribute("targetVersion", "4.5"),
                            new XAttribute("profile", "Full"),
                            new XAttribute("supportedRuntime", "4.0.30319"))
                    ),
                    new XElement(asmv2dependency,
                        new XElement(asmv2dependentAssembly,
                            new XAttribute("dependencyType", "install"),
                            new XAttribute("codebase", manifest.version + $"\\{manifest.entryPoint.name}.manifest"),
                            new XAttribute("size", manifestSize),
                            GetManifestAssemblyIdentity(asmv2assemblyIdentity, manifest),
                            new XElement(asmv2hash,
                                new XElement(dsigTransforms,
                                    new XElement(dsigTransform,
                                        new XAttribute("Algorithm", "urn:schemas-microsoft-com:HashTransforms.Identity"))),
                                new XElement(dsigDigestMethod,
                                    new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha256")),
                                new XElement(dsigDigestValue, manifestDigest)))
                    )
                ));

            if (string.IsNullOrWhiteSpace(manifest.DeploymentProviderUrl))
                document.Descendants(asmv2deploymentProvider).Single().Remove();
            return document;
        }
    }
}
