using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Packager
{
    partial class Program
    {
        private const string CONST_HASH_TRANSFORM_IDENTITY = "urn:schemas-microsoft-com:HashTransforms.Identity";
        private const string CONST_NULL_PUBKEY = "0000000000000000";

        static void Main(string[] args)
        {
            Console.WriteLine("Packager.exe invoked with:");
            Console.WriteLine($"\tArgs={string.Join("|", args)}");
            Console.WriteLine($"\tWorking Directory={Environment.CurrentDirectory}");
            if (args.Length == 0)
            {
                Console.WriteLine("No target specified.");
                return;
            }
            else if (File.Exists(args[0]))
            {
                Console.WriteLine($"Packaging {args[0]}");
            }
            var project = new FileInfo(args[0]).FullName;

            var directory = new DirectoryInfo(Path.GetDirectoryName(project));
            var target = directory.CreateSubdirectory("_publish");

            var resources = new Resources(project);
            resources.StripManifest();

            var date = DateTime.UtcNow;
            var major = date.ToString("yyMM");
            var minor = date.ToString("ddHH");
            var patch = date.ToString("mmss");
            var build = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "0";
            var manifest = new Manifest()
            {
                version = major + "." + minor + "." + patch + "." + build
            };

            target = target.CreateSubdirectory(manifest.version);
            EnumerateFiles(directory, manifest);
            manifest.entryPoint = manifest.files.Single(n => n.Name == Path.GetFileName(project));
            //if (manifest.iconFile == null)
            //{
            //    var icon = resources.ExtractIcon(project);
            //    if (icon != null)
            //    {
            //        manifest.files.Add(icon);
            //    }
            //    manifest.iconFile = icon.Name;
            //}

            var xml = GenerateManifest(directory, manifest);
            string manifestPath = Path.Combine(target.FullName, Path.GetFileName(project) + ".manifest");
            File.WriteAllText(manifestPath, xml.ToString(SaveOptions.OmitDuplicateNamespaces));

            foreach (var file in manifest.files)
            {
                File.Copy(Path.Combine(directory.FullName, file.Name), Path.Combine(target.FullName, file.Name), true);
            }
            xml = GenerateApplicationManifest(manifest, File.ReadAllBytes(manifestPath));
            File.WriteAllText(Path.Combine(target.FullName, Path.GetFileName(project) + ".application"), xml.ToString(SaveOptions.OmitDuplicateNamespaces));
            File.Copy(Path.Combine(target.FullName, Path.GetFileName(project) + ".application"), Path.Combine(directory.FullName, "_publish", Path.GetFileName(project) + ".application"), true);
        }

        private static void EnumerateFiles(DirectoryInfo directory, Manifest manifest)
        {
            manifest.files = new List<ManifestFile>();

            Stack<FileInfo> content = new Stack<FileInfo>();

            foreach (var file in directory.EnumerateFiles())
            {
                if (file.Name.Contains(".vshost"))
                    continue;

                if (!(file.Extension == ".dll" || file.Extension == ".exe"))
                {
                    content.Push(file);
                    continue;
                }
                Console.WriteLine("Processing " + file.Name + "...");

                manifest.files.Add(new ManifestFile(file));


            }

            foreach (var file in content)
            {
                if (file.Name.Contains(".vshost"))
                    continue;
                Console.WriteLine($"Adding file {file.Name}");
                if (file.Extension == ".ico" && string.IsNullOrEmpty(manifest.iconFile))
                    manifest.iconFile = file.Name;
                manifest.files.Add(new ManifestFile(file));
            }
        }

        private static XDocument GenerateManifest(DirectoryInfo directory, Manifest manifest)
        {
            var documentElements = new List<object>
            {
                new XAttribute(XNamespace.Xmlns + "asmv1", Xmlns.asmv1ns),
                new XAttribute("xmlns", Xmlns.asmv2ns),
                new XAttribute(XNamespace.Xmlns + "asmv3ns", Xmlns.asmv3ns),
                new XAttribute(XNamespace.Xmlns + "dsig", Xmlns.dsigns),
                new XAttribute("manifestVersion", "1.0"),
                GetManifestAssemblyIdentity(Xmlns.asmv1assemblyIdentity, manifest, false),
                new XElement(Xmlns.asmv2application),
                new XElement(Xmlns.asmv2entryPoint,
                    GetDependencyAssemblyIdentity(manifest.entryPoint),
                    new XElement(Xmlns.asmv2commandLine,
                        new XAttribute("file", manifest.entryPoint.Name),
                        new XAttribute("parameters", ""))
                ),
                new XElement(Xmlns.asmv2trustInfo,
                    new XElement(Xmlns.asmv2security,
                        new XElement(Xmlns.asmv2applicationRequestMinimum,
                            new XElement(Xmlns.asmv2PermissionSet,
                                new XAttribute("Unrestricted", "true"),
                                new XAttribute("ID", "Custom"),
                                new XAttribute("SameSite", "site")),
                            new XElement(Xmlns.asmv2defaultAssemblyRequest,
                                new XAttribute("permissionSetReference", "Custom"))),
                        new XElement(Xmlns.asmv3requestedPrivileges,
                            new XElement(Xmlns.asmv3requestedExecutionLevel,
                                new XAttribute("level", "asInvoker"),
                                new XAttribute("uiAccess", "false"))))
                ),
                // For reasons I don't quite understand, all clickonce manifests are marked compatible with XP+ (even those on Framework 4.6+)
                new XElement(Xmlns.asmv2dependency,
                    new XElement(Xmlns.asmv2dependentOS,
                        new XElement(Xmlns.asmv2osVersionInfo,
                            new XElement(Xmlns.asmv2os,
                                new XAttribute("majorVersion", "5"),
                                new XAttribute("minorVersion", "1"),
                                new XAttribute("buildNumber", "2600"),
                                new XAttribute("servicePackMajor", "0"))))
                ),
                new XElement(Xmlns.asmv2dependency,
                    new XElement(Xmlns.asmv2dependentAssembly,
                        new XAttribute("dependencyType", "preRequisite"),
                        new XAttribute("allowDelayedBinding", "true"),
                        new XElement(Xmlns.asmv2assemblyIdentity,
                            new XAttribute("name", "Microsoft.Windows.CommonLanguageRuntime"),
                            new XAttribute("version", "4.0.30319.0")))
                )
            };
            
            foreach (var item in manifest.files)
            {
                if (item.Version != null)
                {
                    documentElements.Add(
                        new XElement(Xmlns.asmv2dependency,
                            new XElement(Xmlns.asmv2dependentAssembly,
                                new XAttribute("dependencyType", "install"),
                                new XAttribute("allowDelayedBinding", "true"),
                                new XAttribute("codebase", item.Name.Replace("/", "\\")),
                                new XAttribute("size", item.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                GetDependencyAssemblyIdentity(item),
                                new XElement(Xmlns.asmv2hash,
                                    new XElement(Xmlns.dsigTransforms,
                                        new XElement(Xmlns.dsigTransform,
                                            new XAttribute("Algorithm", CONST_HASH_TRANSFORM_IDENTITY))),
                                    new XElement(Xmlns.dsigDigestMethod,
                                        new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#" + item.DigestMethod)),
                                    new XElement(Xmlns.dsigDigestValue,
                                        new XText(item.DigestValue))))));
                }
                else
                {
                    documentElements.Add(
                        new XElement(Xmlns.asmv2file,
                            new XAttribute("name", item.Name.Replace("/", "\\")),
                            new XAttribute("size", item.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                            new XElement(Xmlns.asmv2hash,
                                new XElement(Xmlns.dsigTransforms,
                                    new XElement(Xmlns.dsigTransform,
                                        new XAttribute("Algorithm", CONST_HASH_TRANSFORM_IDENTITY))),
                                new XElement(Xmlns.dsigDigestMethod,
                                    new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#" + item.DigestMethod)),
                                new XElement(Xmlns.dsigDigestValue,
                                    new XText(item.DigestValue)))));
                }
            }
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(Xmlns.asmv1assembly, documentElements));
        }

        private static XElement GetManifestAssemblyIdentity(XName asmvxassemblyIdentity, Manifest manifest, bool useEntryPoint)
        {
            if (useEntryPoint)
            {
                return new XElement(asmvxassemblyIdentity,
                    new XAttribute("name", manifest.entryPoint.Name),
                    new XAttribute("version", manifest.entryPoint.Version),
                    new XAttribute("publicKeyToken", manifest.entryPoint.PublicKeyToken ?? CONST_NULL_PUBKEY),
                    new XAttribute("language", "neutral"),
                    new XAttribute("processorArchitecture", "msil") // TODO: Identify Architecture
                    //new XAttribute("type", "win32") // TODO: This too.
                );
            }
            else
            {
                return new XElement(asmvxassemblyIdentity,
                new XAttribute("name", manifest.entryPoint.Name),
                new XAttribute("version", manifest.version),
                new XAttribute("publicKeyToken", CONST_NULL_PUBKEY),
                new XAttribute("language", "neutral"),
                new XAttribute("processorArchitecture", "msil"),
                new XAttribute("type", "win32")
            );
            }
        }

        private static XElement GetDependencyAssemblyIdentity(ManifestFile file)
        {
            var assemblyIdentityAttributes = new List<XAttribute>
            {
                new XAttribute("name", file.AssemblyName ?? file.Name.Substring(0, file.Name.LastIndexOf("."))),
                new XAttribute("version", file.Version),
                new XAttribute("language", "neutral"),
                new XAttribute("processorArchitecture", file.Architecture.ToString("G").ToLowerInvariant()),
            };

            if (file.PublicKeyToken != null)
            {
                assemblyIdentityAttributes.Add(
                    new XAttribute("publicKeyToken", file.PublicKeyToken));
            }

            return new XElement(Xmlns.asmv2assemblyIdentity, assemblyIdentityAttributes);
        }

        public static XDocument GenerateApplicationManifest(Manifest manifest, byte[] manifestBytes)
        {
            var manifestSize = manifestBytes.Length;
            var manifestDigest = Crypto.GetSha256DigestValue(manifestBytes);

            if (string.IsNullOrWhiteSpace(manifest.entryPoint.Publisher))
                manifest.entryPoint.Publisher = Environment.UserName;
            if (string.IsNullOrWhiteSpace(manifest.entryPoint.Product))
                manifest.entryPoint.Product = manifest.entryPoint.Name;

            var document = new XDocument(
               new XDeclaration("1.0", "utf-8", null),
               new XElement(Xmlns.asmv1assembly,
                   new XAttribute(XNamespace.Xmlns + "asmv1", Xmlns.asmv1ns),
                   new XAttribute(XNamespace.Xmlns + "asmv2", Xmlns.asmv2ns),
                   new XAttribute(XNamespace.Xmlns + "co.v2", Xmlns.clickoncev2ns),
                   new XAttribute(XNamespace.Xmlns + "co.v1", Xmlns.clickoncev1ns),
                   new XAttribute(XNamespace.Xmlns + "dsig", Xmlns.dsigns),
                   new XAttribute("manifestVersion", "1.0"),
                   new XElement(Xmlns.asmv1assemblyIdentity,
                       new XAttribute("name", Path.ChangeExtension(manifest.entryPoint.Name, ".application")),
                       new XAttribute("version", manifest.version),
                       new XAttribute("publicKeyToken", "0000000000000000"),
                       new XAttribute("language", "neutral"),
                       new XAttribute("processorArchitecture", "msil")
                   ),
                   ManifestDescription(manifest),
                   new XElement(Xmlns.asmv2deployment,
                       new XAttribute("install", "true"),
                       new XAttribute("mapFileExtensions", "false"),
                       new XAttribute("trustURLParameters", "true"),
                       new XAttribute(Xmlns.clickoncev1createDesktopShortcut, true),
                   new XElement(Xmlns.asmv2subscription,
                       new XElement(Xmlns.asmv2update,
                           new XElement(Xmlns.asmv2beforeApplicationStartup))),
                       new XElement(Xmlns.asmv2deploymentProvider,
                           new XAttribute("codebase", manifest.DeploymentProviderUrl))
                   ),
                   new XElement(Xmlns.clickoncev2compatibleFrameworks,
                       new XElement(Xmlns.clickoncev2framework,
                           new XAttribute("targetVersion", "4.5"),
                           new XAttribute("profile", "Full"),
                           new XAttribute("supportedRuntime", "4.0.30319"))
                   ),
                   new XElement(Xmlns.asmv2dependency,
                       new XElement(Xmlns.asmv2dependentAssembly,
                           new XAttribute("dependencyType", "install"),
                           new XAttribute("codebase", manifest.version + $"\\{manifest.entryPoint.Name}.manifest"),
                           new XAttribute("size", manifestSize),
                           GetManifestAssemblyIdentity(Xmlns.asmv2assemblyIdentity, manifest, false),
                           new XElement(Xmlns.asmv2hash,
                               new XElement(Xmlns.dsigTransforms,
                                   new XElement(Xmlns.dsigTransform,
                                       new XAttribute("Algorithm", "urn:schemas-microsoft-com:HashTransforms.Identity"))),
                               new XElement(Xmlns.dsigDigestMethod,
                                   new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#sha256")),
                               new XElement(Xmlns.dsigDigestValue, manifestDigest)))
                   )
               ));

            if (string.IsNullOrWhiteSpace(manifest.DeploymentProviderUrl))
                document.Descendants(Xmlns.asmv2deploymentProvider).Single().Remove();
            return document;
        }

        private static XElement ManifestDescription(Manifest manifest)
        {
            return new XElement(Xmlns.asmv1description,
                                    new XAttribute(Xmlns.asmv2publisher, manifest.entryPoint.Publisher),
                                    new XAttribute(Xmlns.asmv2product, manifest.entryPoint.Product)
                                );
        }
    }
}
