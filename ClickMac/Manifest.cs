using System;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace ClickMac
{
    internal class Manifest
    {
        public static string GetUrlFolder(string url)
        {
            return (url = new Uri(url).ToString()).Substring(0, url.LastIndexOf('/'));
        }
        public static string FixFileSeperator(string path)
        {
            if (String.IsNullOrEmpty(path))
                return path;
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }


        public XDocument Xml;
        public String Location;
        public string DiskLocation;
        public string Subfolder;

        private EntryPoint entry = new EntryPoint();
        public EntryPoint Entry { get { return entry; } }

        public List<Manifest> Children { get; private set; } = new List<Manifest>();

        public Manifest(string Uri, string subfolder = "")
        {
            Subfolder = subfolder;
            Location = Uri;
            Loading.Log($"Loading Manifest from {Uri}");
            try
            {
                Xml = XDocument.Load(Uri);
            }
            catch (WebException) when (Platform.IsRunningOnMono)
            {
                using (var curl = new CurlWrapper())
                {
                    FileInfo tempFile = curl.GetFile(Location);
                    FileStream fileStream = tempFile.OpenRead();
                    Xml = XDocument.Load(fileStream, LoadOptions.PreserveWhitespace);
                }
            }
            DiskLocation = Xml.Root.Element(Namespace.XName("assemblyIdentity", ns.asmv1)).Attribute("name").Value;

            var deployment = Xml.Root.Element(Namespace.XName("deployment", ns.asmv2));
            if (deployment != null && deployment.Element(Namespace.XName("deploymentProvider", ns.asmv2)) != null)
            {
                UpdateManifest(deployment);
            }
        }

        private void UpdateManifest(XElement deployment)
        {
            var updateLocation = deployment.Element(Namespace.XName("deploymentProvider", ns.asmv2)).Attribute("codebase").Value;

            var PublisherIdentity = Xml.Root.Element(Namespace.XName("publisherIdentity", ns.asmv2));
            if (PublisherIdentity == null)
            {
                // Deployed with no security. Blindly update.
                Location = updateLocation;
                XDocument newManifest = null;

                Loading.Log("Getting updated manifest from {0}", Location);
                try
                {
                    newManifest = XDocument.Load(new WebClient().OpenRead(Location), LoadOptions.PreserveWhitespace);
                    newManifest.Save(DiskLocation);
                    Xml = newManifest;
                }
                catch (WebException c) when (Platform.IsRunningOnMono && c.Status == WebExceptionStatus.SecureChannelFailure)
                {
                    using (var curl = new CurlWrapper())
                    {
                        FileInfo tempFile = curl.GetFile(Location);
                        FileStream fileStream = tempFile.OpenRead();
                        newManifest = XDocument.Load(fileStream, LoadOptions.PreserveWhitespace);
                        newManifest.Save(DiskLocation);
                        fileStream.Close();
                        tempFile.Delete();
                    }
                }
                catch (WebException e)
                {
                    Loading.Log("Getting manifest failed.");
                    Loading.Log($"\t{e.Status}");
                    Loading.Log($"\t{e}");

                }
                catch (XmlException e)
                {
                    Loading.Log("Getting manifest failed.");
                    Loading.Log($"\t{e}");
                }
            }
            else
            {
                if (!VerifySignature(true))
                {
                    Loading.Log("Failed to validate XML signature");
                    return;
                }
                Xml = XDocument.Load(DiskLocation);
            }                
            entry.DeploymentProviderUrl = Location;
            
        }

        private bool VerifySignature(bool Update)
        {
            var xdoc = new XmlDocument()
            {
                PreserveWhitespace = true
            };
            xdoc.Load(Location);
            SignedXml signed = new SignedXml(xdoc);

            var pubName = xdoc.GetElementsByTagName("publisherIdentity")[0].Attributes["name"].Value;
            var pubHash = xdoc.GetElementsByTagName("publisherIdentity")[0].Attributes["issuerKeyHash"].Value;

            XmlNodeList nodeList = xdoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (nodeList.Count == 0)
                return false; // This code should never have been called. Something's probably wrong.  Fail it.
            signed.LoadXml((XmlElement)nodeList[0]);
            AsymmetricAlgorithm key;
            var validSignature = signed.CheckSignatureReturningKey(out key);
            if (validSignature && Update)
            {
                var updatedLocation = 
                    (xdoc.GetElementsByTagName("deployment")[0] as XmlElement)
                         .GetElementsByTagName("deploymentProvider")[0].Attributes["codebase"].Value;

                xdoc = new XmlDocument()
                {
                    PreserveWhitespace = true
                };
                xdoc.Load(updatedLocation);
                if (
                    pubName != xdoc.GetElementsByTagName("publisherIdentity")[0].Attributes["name"].Value || 
                    pubHash != xdoc.GetElementsByTagName("publisherIdentity")[0].Attributes["issuerKeyHash"].Value)
                {
                    // Different publisher.
                    return false;
                }

                signed = new SignedXml(xdoc);

                nodeList = xdoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
                if (nodeList.Count == 0)
                    return false; // Signature was removed. Don't trust new version.
                signed.LoadXml((XmlElement)nodeList[0]);
                validSignature = signed.CheckSignatureReturningKey(out key);
                if (validSignature)
                {
                    xdoc.Save(DiskLocation);
                    Location = updatedLocation;
                }
            }
            return validSignature;
        }

        public void ProcessDependencies()
        {
            var path = GetUrlFolder(Location);
            foreach (var dependency in Xml.Root.Elements(Namespace.XName("dependency", ns.asmv2)))
            {
                ProcessDependency(dependency, path);
            }
        }

        private void ProcessDependency(XElement dependency, string path)
        {
            var dependentAssembly = dependency.Element(Namespace.XName("dependentAssembly", ns.asmv2));
            if (dependentAssembly == null || dependentAssembly.Attribute("dependencyType").Value != "install")
                return;
            var codebase = FixFileSeperator(dependentAssembly.Attribute("codebase").Value);
            var assemblyIdentity = dependentAssembly.Element(Namespace.XName("assemblyIdentity", ns.asmv2));
            string version = String.Format("{0}_{1}", assemblyIdentity.Attribute("name").Value, assemblyIdentity.Attribute("version").Value);
            Directory.CreateDirectory(version);
            try
            {
                foreach (var deploy in Directory.EnumerateFiles(version, "*.deploy", SearchOption.AllDirectories))
                {
                    var dest = deploy.Substring(0, deploy.Length - ".deploy".Length);
                    if (File.Exists(dest))
                        File.Delete(dest);
                    File.Move(deploy, dest);
                }
            }
            catch (IOException) { }
            string filename = Path.Combine(".", version, Path.GetFileName(codebase));
            bool downloaded = false;
            if (File.Exists(filename))
            {
                if (new FileInfo(filename).Length == int.Parse(dependentAssembly.Attribute("size").Value)) // HACK: Not an actual equality test. (Although it's usually good enough)
                {
                    downloaded = true;
                }
                else
                {
                    if (File.Exists(filename + "._"))
                        File.Delete(filename + "._");
                    File.Move(filename, filename + "._");
                }
            }
            if (!downloaded)
            {
                Loading.Log("Getting Dependency {0}", codebase);
                DownloadFile(path, codebase, filename);
            }
            if (File.Exists(filename + "._"))
                File.Delete(filename + "._");
            if (Path.GetExtension(codebase) == ".manifest")
            {
                var manifest = new Manifest(path + "/" + codebase.Replace('\\', '/'), version);
                manifest.ProcessDependencies();
                Children.Add(manifest);
                entry.Import(manifest.entry);
            }
            foreach (var file in Xml.Root.Elements(Namespace.XName("file", ns.asmv2)))
            {
                GetFile(file, GetUrlFolder(path + "/" + codebase.Replace('\\', '/')));
            }
            foreach (var fa in Xml.Root.Elements(Namespace.XName("fileAssociation", ns.cov1)))
            {
                Platform.AssociateFile(fa, this);
            }
            var entryPoint = Xml.Root.Element(Namespace.XName("entryPoint", ns.asmv2));
            if (entryPoint != null)
            {
                entry.executable = entryPoint.Element(Namespace.XName("commandLine", ns.asmv2)).Attribute("file").Value;
                entry.folder = new DirectoryInfo(Subfolder ?? version).FullName; // Alsolute reference.
                entry.version = assemblyIdentity.Attribute("version").Value;
                entry.displayName = entryPoint.Element(Namespace.XName("assemblyIdentity", ns.asmv2)).Attribute("name").Value;
            }
            var description = Xml.Root.Element(Namespace.XName("description", ns.asmv1));
            if (description != null)
            {
                var iconFile = description.Attribute(Namespace.XName("iconFile", ns.asmv2));
                if (iconFile != null && !string.IsNullOrWhiteSpace(iconFile.Value))
                {
                    if (File.Exists(Path.Combine(Subfolder ?? version, iconFile.Value)))
                    {
                        entry.icon = Path.Combine(Subfolder ?? version, iconFile.Value);
                    }
                }
            }
            if (!String.IsNullOrWhiteSpace(Subfolder))
            {
                if (!File.Exists(Path.Combine(Subfolder, Path.GetFileName(codebase))))
                    File.Copy(Path.Combine(".", version, Path.GetFileName(codebase)), Path.Combine(Subfolder, Path.GetFileName(codebase)));
            }
        }

        private void GetFile(XElement file, string path)
        {

            string name = file.Attribute("name").Value;
            string filename = Path.Combine(Subfolder, name.Replace('\\', Path.DirectorySeparatorChar));
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            Console.WriteLine("Getting {0}", filename);
            bool downloaded = false;
            if (File.Exists(filename))
            {
                if (new FileInfo(filename).Length == int.Parse(file.Attribute("size").Value))
                {
                    downloaded = true;
                }
                else
                    File.Move(filename, filename + "._");
            }
            if (!downloaded)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                try
                {
                    new WebClient().DownloadFile(path + "/" + name.Replace('\\', '/'), filename);
                }
                catch (WebException) when (Platform.IsRunningOnMono)
                {
                    using (var curl = new CurlWrapper())
                    {
                        FileInfo tempFile = curl.GetFile(Location);
                        tempFile.MoveTo(filename);
                        Console.WriteLine(tempFile.FullName);
                    }
                }
                catch (WebException)
                {
                    try
                    {
                        new WebClient().DownloadFile(path + "/" + name.Replace('\\', '/') + ".deploy", filename);
                    }
                    catch (WebException)
                    {
                        if (File.Exists(filename + "._"))
                            File.Move(filename + "._", filename);
                        else
                        {
                            Loading.Log("\tFailed to download!  Application might not work.");
                            return;
                        }
                    }
                }
                if (File.Exists(filename + "._"))
                    File.Delete(filename + "._");
            }
        }

        string DownloadFile(string path, string codebase, string filename)
        {
            var urls = new string[] {
                path + "/" + codebase.Replace('\\', '/'),                                       // Expected value
                path + "/" + codebase.Replace('\\', '/') + ".deploy",                           // Escaped value
                path + "/" + Path.GetFileName(codebase.Replace('\\', '/')),                     // Shallow path
                path + "/" + Path.GetFileName(codebase.Replace('\\', '/')) + ".deploy",         // Escaped Shallow path

            };

            foreach (var url in urls)
            {
                try
                {
                    Loading.Log($"> {url}");
                    new WebClient().DownloadFile(url, filename);
                }
                catch (WebException c) when (Platform.IsRunningOnMono && c.Status == WebExceptionStatus.SecureChannelFailure)
                {
                    using (var curl = new CurlWrapper())
                    {
                        FileInfo tempFile = curl.GetFile(url);
                        if (tempFile == null)
                            continue;
                        tempFile.MoveTo(filename);
                        Console.WriteLine(tempFile.FullName);
                    }
                }
                catch (WebException)
                {
                    continue;
                }
                return filename;
            }
            if (File.Exists(filename + "._"))
            {
                File.Move(filename + "._", filename);
                return filename;
            }
            else
            {
                Loading.Log("\tFailed to download!  Application might not work.");
                return null;
            }
        }
    }
}

