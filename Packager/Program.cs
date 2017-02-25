using ClickMono.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Packager
{
    partial class Program
    {
        enum ExitCodes { OK = 0, InvalidArgs = 1, }
        private const string CONST_HASH_TRANSFORM_IDENTITY = "urn:schemas-microsoft-com:HashTransforms.Identity";
        private const string CONST_NULL_PUBKEY = "0000000000000000";

        public StartupOptions Options { get; private set; }

        static int Main(string[] args)
        {
            Console.WriteLine("Packager.exe invoked with:");
            Console.WriteLine($"\tArgs={string.Join("|", args)}");
            Console.WriteLine($"\tWorking Directory={Environment.CurrentDirectory}");
            var program = new Program();
            if (args.Length == 0)
            {
                Console.WriteLine("No target specified.");
                return 1;
            }
            else if (args.Length == 1 && File.Exists(args[0]))
            {
                var project = new FileInfo(args[0]).FullName;
                Console.WriteLine($"Packaging {project}");
                program.Options = new StartupOptions
                {
                    Target = project,
                    Mode = StartupOptions.Modes.Generate
                };
            }
            else
            {
                program.Options = new StartupOptions();
                IterateArgs(args, program.Options);
            }
            return program.Run();
        }

        private int Run()
        {
            int res = 0;
            switch (Options.Mode)
            {
                case StartupOptions.Modes.Generate:
                    res = Generate();
                    break;
                case StartupOptions.Modes.Update:
                    res = Update();
                    break;
            }
            if (res > 0)
                return res;
            if (!string.IsNullOrEmpty(Options.GenerateBootstrap))
            {
                res = Bootstrapper.GenerateBootstrap(Options.DeploymentProvider, Options.GenerateBootstrap);
            }
            if (res > 0)
                return res;

            return res;
        }

        static void IterateArgs(IEnumerable<string> args, StartupOptions options)
        {
            while (args.Any())
            {
                switch (args.First())
                {
                    case "--generate":
                        options.Mode = StartupOptions.Modes.Generate;
                        args = args.Skip(1);
                        options.Target = args.First();
                        args = args.Skip(1);
                        break;
                    case "--update":
                        options.Mode = StartupOptions.Modes.Update;
                        args = args.Skip(1);
                        options.Target = args.First();
                        args = args.Skip(1);
                        break;
                    case "--deploymentProvider":
                    case "--deploymentUrl":
                        options.DeploymentProvider = args.ElementAt(1);
                        args = args.Skip(2);
                        break;
                    case "--generateBootstrap":
                    case "--generateSetup":
                        options.GenerateBootstrap = args.ElementAt(1);
                        args = args.Skip(2);
                        break;
                    default:
                        Console.WriteLine($"UNKNOWN ARG: {args.First()}");
                        Environment.Exit(1);
                        break;

                }
            }
        }

        public int Generate()
        {
            var project = Options.Target;
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
                Version = major + "." + minor + "." + patch + "." + build
            };

            target = target.CreateSubdirectory(manifest.Version);
            EnumerateFiles(directory, manifest);
            manifest.entryPoint = manifest.Files.Single(n => n.Name == Path.GetFileName(project));

            var xml = GenerateManifest(directory, manifest);
            var manifestPath = Path.Combine(target.FullName, Path.GetFileName(project) + ".manifest");

            File.WriteAllText(manifestPath, xml.ToString(SaveOptions.OmitDuplicateNamespaces));

            foreach (var file in manifest.Files)
            {
                File.Copy(Path.Combine(directory.FullName, file.Name), Path.Combine(target.FullName, file.Name), true);
            }
            xml = GenerateDeploymentManifest(manifest, File.ReadAllBytes(manifestPath));
            File.WriteAllText(Path.Combine(directory.FullName, "_publish", Path.GetFileNameWithoutExtension(project) + ".application"), xml.ToString(SaveOptions.OmitDuplicateNamespaces));
            return 0;
        }

        private int Update()
        {
            bool deploymentUpdated = false;
            //bool applicationUpdated = false;

            var DeploymentManifest = XDocument.Load(Options.Target);

            if (!String.IsNullOrEmpty(Options.DeploymentProvider))
            {
                var deployment = DeploymentManifest.Descendants(Xmlns.asmv2deployment).SingleOrDefault();
                if (deployment.Elements(Xmlns.asmv2deploymentProvider) != null)
                {
                    deployment.Elements(Xmlns.asmv2deploymentProvider).Single().Attribute("codebase").Value = Options.DeploymentProvider;
                }
                else
                {
                    deployment.Add(new XElement(Xmlns.asmv2deploymentProvider, new XAttribute("codebase", Options.DeploymentProvider)));
                }
                deploymentUpdated = true;
            }

            if (deploymentUpdated)
            {
                DeploymentManifest.Save(Options.Target);
            }
            return 0;
        }

        private static void EnumerateFiles(DirectoryInfo directory, Manifest manifest)
        {
            manifest.Files = new List<ManifestFile>();

            var content = new Stack<FileInfo>();

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

                manifest.Files.Add(new ManifestFile(file));


            }

            foreach (var file in content)
            {
                if (file.Name.Contains(".vshost"))
                    continue;
                Console.WriteLine($"Adding file {file.Name}");
                if (file.Extension.ToLowerInvariant().Trim('.') == "ico" && string.IsNullOrEmpty(manifest.iconFile))
                    manifest.iconFile = file.Name;
                manifest.Files.Add(new ManifestFile(file));
            }
        }

        private static XDocument GenerateManifest(DirectoryInfo directory, Manifest manifest)
        {
            var documentElements = new List<XObject>
            {
                new XAttribute(XNamespace.Xmlns + "asmv1", Xmlns.asmv1ns),
                new XAttribute("xmlns", Xmlns.asmv2ns),
                new XAttribute(XNamespace.Xmlns + "asmv3ns", Xmlns.asmv3ns),
                new XAttribute(XNamespace.Xmlns + "dsig", Xmlns.dsigns),
                new XAttribute("manifestVersion", "1.0"),
                GetManifestAssemblyIdentity(Xmlns.asmv1assemblyIdentity, manifest, false),
                new XElement(Xmlns.asmv1description,
                    new XAttribute(Xmlns.asmv2iconFile, manifest.iconFile ?? manifest.entryPoint.Name)
                ),
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
                )
                //new XElement(Xmlns.asmv2dependency,
                //    new XElement(Xmlns.asmv2dependentAssembly,
                //        new XAttribute("dependencyType", "preRequisite"),
                //        new XAttribute("allowDelayedBinding", "true"),
                //        new XElement(Xmlns.asmv2assemblyIdentity,
                //            new XAttribute("name", "Microsoft.Windows.CommonLanguageRuntime"),
                //            new XAttribute("version", "4.0.30319.0")))
                //)
            };

            foreach (var item in manifest.Files)
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
            //if (manifest.iconFile == null)
            //    documentElements.OfType<XElement>().Single(e => e.Name == Xmlns.asmv1description).Remove();
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
                new XAttribute("version", manifest.Version),
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

        /// <summary>
        /// Generates the Deployment Manifest (.application file)
        /// </summary>
        /// <param name="manifest"></param>
        /// <param name="manifestBytes"></param>
        /// <returns></returns>
        public static XDocument GenerateDeploymentManifest(Manifest manifest, byte[] manifestBytes)
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
                       new XAttribute("version", manifest.Version),
                       new XAttribute("publicKeyToken", "0000000000000000"),
                       new XAttribute("language", "neutral"),
                       new XAttribute("processorArchitecture", "msil")
                   ),
                   ManifestDescription(manifest),
                   DeploymentNode(manifest),
                   new XElement(Xmlns.clickoncev2compatibleFrameworks,
                       new XElement(Xmlns.clickoncev2framework,
                           new XAttribute("targetVersion", "4.5"),
                           new XAttribute("profile", "Full"),
                           new XAttribute("supportedRuntime", "4.0.30319"))
                   ),
                   new XElement(Xmlns.asmv2dependency,
                       new XElement(Xmlns.asmv2dependentAssembly,
                           new XAttribute("dependencyType", "install"),
                           new XAttribute("codebase", manifest.Version + $"\\{manifest.entryPoint.Name}.manifest"),
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

            if (string.IsNullOrWhiteSpace(manifest.Deployment.ProviderUrl))
                document.Descendants(Xmlns.asmv2deploymentProvider).Single().Remove();
            return document;
        }

        private static XElement DeploymentNode(Manifest manifest)
        {
            XElement UpdateNode;
            XElement Deployment = new XElement(Xmlns.asmv2deployment,
                                   new XAttribute("install", manifest.Deployment.Install),
                                   new XAttribute("mapFileExtensions", "false"),
                                   new XAttribute("trustURLParameters", "true"),
                                   new XAttribute(Xmlns.clickoncev1createDesktopShortcut, manifest.Deployment.CreateDesktopShortcut),
                                new XElement(Xmlns.asmv2subscription,
                                   UpdateNode = new XElement(Xmlns.asmv2update)),
                                   new XElement(Xmlns.asmv2deploymentProvider,
                                       new XAttribute("codebase", manifest.Deployment.ProviderUrl))
                               );
            if (manifest.Deployment.MaximumAge.TotalHours == 0)
            {
                UpdateNode.Add(new XElement(Xmlns.asmv2beforeApplicationStartup));
            }
            else
            {
                UpdateNode.Add(new XElement(Xmlns.asmv2expiration,
                    new XAttribute("maximumAge", manifest.Deployment.MaximumAge.TotalHours),
                    new XAttribute("unit", "hours")));
            }
            return Deployment;
        }

        private static XElement ManifestDescription(Manifest manifest)
        {
            XElement description = new XElement(Xmlns.asmv1description,
                                    new XAttribute(Xmlns.asmv2publisher, manifest.entryPoint.Publisher),
                                    new XAttribute(Xmlns.asmv2product, manifest.entryPoint.Product)
                                );
            return description;
        }
    }
}
