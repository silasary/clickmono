using System;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using ClickMono.Common;

namespace ClickMac
{
    public class Manifest
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
        public readonly DeploymentOptions Options;
        public readonly string Identity;

        public Manifest(string Uri, DeploymentOptions options, string subfolder = "")
        {
            Options = options;
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
                    if (tempFile != null)
                    {
                        FileStream fileStream = tempFile.OpenRead();
                        Xml = XDocument.Load(fileStream, LoadOptions.PreserveWhitespace);
                    }
                }
            }
            DiskLocation = Xml.Root.Element(Xmlns.asmv1assemblyIdentity).Attribute("name").Value;
            Identity = string.Format("{0}_{1}", Path.GetFileNameWithoutExtension(DiskLocation), Xml.Root.Element(Xmlns.asmv1assemblyIdentity).Attribute("version").Value);
            var deployment = Xml.Root.Element(Xmlns.asmv2deployment);
            if (deployment != null)
            {
                XAttribute mapFileExtensions = deployment.Attribute("mapFileExtensions");
                if (bool.Parse(mapFileExtensions?.Value ?? "false"))
                {
                    options.MapFileExtensions = true;
                }
                if (deployment.Element(Xmlns.asmv2deploymentProvider) != null)
                {
                    UpdateManifest(deployment);
                }
            }
        }

        private void UpdateManifest(XElement deployment)
        {
            var updateLocation = deployment.Element(Xmlns.asmv2deploymentProvider).Attribute("codebase").Value;

            var PublisherIdentity = Xml.Root.Element(Xmlns.asmv2publisherIdentity);
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
            XmlNode publisherIdentity = xdoc.GetElementsByTagName("publisherIdentity")[0];
            var pubName = publisherIdentity.Attributes["name"].Value;
            var pubHash = publisherIdentity.Attributes["issuerKeyHash"].Value;

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
                    pubName != publisherIdentity.Attributes["name"].Value || 
                    pubHash != publisherIdentity.Attributes["issuerKeyHash"].Value)
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
            foreach (var dependency in Xml.Root.Elements(Xmlns.asmv2dependency))
            {
                ProcessDependency(dependency, path);
            }
            foreach (var file in Xml.Root.Elements(Xmlns.asmv2file))
            {
                GetFile(file, path);
            }
            foreach (var fa in Xml.Root.Elements(Xmlns.clickoncev1fileAssociation))
            {
                Platform.AssociateFile(fa, this);
            }
            var entryPoint = Xml.Root.Element(Xmlns.asmv2entryPoint);
            if (entryPoint != null)
            {
                entry.executable = entryPoint.Element(Xmlns.asmv2commandLine).Attribute("file").Value;
                entry.folder = new DirectoryInfo(Subfolder).FullName; // Alsolute reference.
                XElement identity = entryPoint.Element(Xmlns.asmv2assemblyIdentity);
                entry.version = identity.Attribute("version").Value;
                entry.displayName = identity.Attribute("name").Value;
            }
            var description = Xml.Root.Element(Xmlns.asmv1description);
            if (description != null)
            {
                var iconFile = description.Attribute(Xmlns.asmv2iconFile);
                if (iconFile != null && !string.IsNullOrWhiteSpace(iconFile.Value))
                {
                    if (File.Exists(Path.Combine(Subfolder, iconFile.Value)))
                    {
                        entry.icon = Path.Combine(Subfolder, iconFile.Value);
                    }
                }
            }
        }

        private void ProcessDependency(XElement dependency, string path)
        {
            var dependentAssembly = dependency.Element(Xmlns.asmv2dependentAssembly);
            if (dependentAssembly == null || dependentAssembly.Attribute("dependencyType").Value != "install")
                return;
            var codebase = FixFileSeperator(dependentAssembly.Attribute("codebase").Value);
            var assemblyIdentity = dependentAssembly.Element(Xmlns.asmv2assemblyIdentity);
            string version = String.Format("{0}_{1}", assemblyIdentity.Attribute("name").Value, assemblyIdentity.Attribute("version").Value);
            string dependancyDirectory = Path.Combine(Platform.LibraryLocation, version);
            Directory.CreateDirectory(dependancyDirectory);
            try
            {
                foreach (var deploy in Directory.EnumerateFiles(dependancyDirectory, "*.deploy", SearchOption.AllDirectories))
                {
                    var dest = deploy.Substring(0, deploy.Length - ".deploy".Length);
                    if (File.Exists(dest))
                        File.Delete(dest);
                    File.Move(deploy, dest);
                }
            }
            catch (IOException) { }

            string filename = Path.Combine(dependancyDirectory, Path.GetFileName(codebase));
            bool invalid = IsExistingFileInvalid(dependentAssembly, filename);
            if (invalid)
            {
                Loading.Log("Getting Dependency {0}", codebase);
                DownloadFile(path, codebase, filename);
            }
            if (File.Exists(filename + "._"))
                File.Delete(filename + "._");
            if (Path.GetExtension(codebase) == ".manifest")
            {
                var manifest = new Manifest(path + "/" + codebase.Replace('\\', '/'), Options, version);
                manifest.ProcessDependencies();
                Children.Add(manifest);
                entry.Import(manifest.entry);
            }
            if (!String.IsNullOrWhiteSpace(Subfolder))
            {
                var dest = Path.Combine(Platform.LibraryLocation, Subfolder, Path.GetFileName(codebase));
                if (!File.Exists(dest))
                {
                    File.Copy(filename, dest);
                }
            }
        }

        private static bool IsExistingFileInvalid(XElement dependentAssembly, string filename)
        {
            bool isInvalid = false;
            if (!File.Exists(filename))
            {
                isInvalid = true;
            }
            else
            {
                int size = int.Parse(dependentAssembly.Attribute("size").Value);
                string digestValue = dependentAssembly.Element(Xmlns.asmv2hash).Element(Xmlns.dsigDigestValue).Value;
                string digestMethod = dependentAssembly.Element(Xmlns.asmv2hash).Element(Xmlns.dsigDigestMethod).Attribute("Algorithm").Value.Split('#')[1];
                if (new FileInfo(filename).Length != size) // HACK: Not an actual equality test. (Although it's usually good enough)
                {
                    isInvalid = true;
                }
                if (!Crypto.AreEqual(filename, digestMethod, digestValue))
                {
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    if (File.Exists(filename + "._"))
                        File.Delete(filename + "._");
                    File.Move(filename, filename + "._");
                }
            }

            return isInvalid;
        }

        private void GetFile(XElement file, string path)
        {

            string name = file.Attribute("name").Value;
            string filename = Path.Combine(Subfolder, name.Replace('\\', Path.DirectorySeparatorChar));
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            Console.WriteLine("Getting {0}", filename);
            bool invalid = IsExistingFileInvalid(file, filename);
            if (invalid)
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
            var url = codebase.Replace('\\', '/') + (Options.MapFileExtensions ? ".deploy" : "");

            try
            {
                Loading.Log($"> {url}");
                WebClient webClient = new WebClient();
                webClient.BaseAddress = path + "/";
                webClient.DownloadFile(url, filename);
                return filename;
            }
            catch (WebException c) when (Platform.IsRunningOnMono && c.Status == WebExceptionStatus.SecureChannelFailure)
            {
                using (var curl = new CurlWrapper())
                {
                    FileInfo tempFile = curl.GetFile(url);
                    if (tempFile != null)
                    {
                        tempFile.MoveTo(filename);
                        Console.WriteLine(tempFile.FullName);
                        return filename;
                    }
                }
            }
            catch (WebException c)
            {
                Options.Errors++;
                Console.WriteLine(c);
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

