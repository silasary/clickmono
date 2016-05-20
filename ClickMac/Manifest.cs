using System;
using System.Xml.Linq;
using System.Net;
using System.IO;

namespace ClickMac
{
    public class Manifest
    {
        enum ns { asmv1, asmv2, asmv3, cov1, cov2, dsig }
        private static XName xname(string localName, ns Ns)
        {
            string NameSpace = "";
            switch (Ns)
            {
                case ns.asmv1:
                    NameSpace = "urn:schemas-microsoft-com:asm.v1";
                    break;
                case ns.asmv2:
                    NameSpace = "urn:schemas-microsoft-com:asm.v2";
                    break;
                case ns.asmv3:
                    NameSpace = "urn:schemas-microsoft-com:asm.v3";
                    break;
                case ns.cov1:
                    NameSpace = "urn:schemas-microsoft-com:clickonce.v1";
                    break;
                case ns.cov2:
                    NameSpace = "urn:schemas-microsoft-com:clickonce.v2";
                    break;
                case ns.dsig:
                    NameSpace = "http://www.w3.org/2000/09/xmldsig#";
                    break;
            }
            return XName.Get(localName, NameSpace);
        }
        public static string getUrlFolder(string url)
        {
            return url.Substring(0, url.LastIndexOf('/'));
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

        public EntryPoint entry = new EntryPoint();


        public Manifest(string Uri, string subfolder = "")
        {
            Subfolder = subfolder;
            Location = Uri;
            Xml = XDocument.Load(Uri);
            DiskLocation = Xml.Root.Element(xname("assemblyIdentity", ns.asmv1)).Attribute("name").Value;

            var deployment = Xml.Root.Element(xname("deployment", ns.asmv2));
            if (deployment.Element(xname("deploymentProvider", ns.asmv2)) != null)
            {
                var provider = deployment.Element(xname("deploymentProvider", ns.asmv2)).Attribute("codebase").Value;
                entry.DeploymentProviderUrl = provider;
                XDocument newManifest = null;
                try
                {
                    Loading.Log("Getting updated manifest from {0}", provider);
                    newManifest = XDocument.Load(new WebClient().OpenRead(provider));
                    newManifest.Save(DiskLocation);
                }
                catch (WebException)
                {
                    Loading.Log("Getting manifest failed. Starting in Offline Mode");
                    newManifest = XDocument.Load(DiskLocation);
                }
            }
        }

        public void ProcessDependencies()
        {
            var path = getUrlFolder(Location);
            foreach (var dependency in Xml.Root.Elements(xname("dependency", ns.asmv2)))
            {
                ProcessDependency(dependency, path);
            }
        }

        private void ProcessDependency(XElement dependency, string path)
        {
            var dependentAssembly = dependency.Element(xname("dependentAssembly", ns.asmv2));
            if (dependentAssembly == null || dependentAssembly.Attribute("dependencyType").Value != "install")
                return;
            var codebase = FixFileSeperator(dependentAssembly.Attribute("codebase").Value);
            var assemblyIdentity = dependentAssembly.Element(xname("assemblyIdentity", ns.asmv2));
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
                    File.Move(filename, filename + "._");
            }
            if (!downloaded)
            {
                try
                {
                    Log("Getting Dependency {0}", codebase);
                    new WebClient().DownloadFile(path + "/" + codebase.Replace('\\', '/'), filename);
                }
                catch (WebException)
                {
                    try
                    {
                        new WebClient().DownloadFile(path + "/" + codebase.Replace('\\', '/') + ".deploy", filename);
                    }
                    catch (WebException)
                    {
                        if (File.Exists(filename + "._"))
                            File.Move(filename + "._", filename);
                        else
                        {
                            Log("\tFailed to download!  Application might not work.");
                            return;
                        }
                    }
                }
            }
            if (File.Exists(filename + "._"))
                File.Delete(filename + "._");
            if (Path.GetExtension(codebase) == ".manifest")
            {
                var manifest = new Manifest(Path.Combine(".", version, Path.GetFileName(codebase)), version);
                manifest.ProcessDependencies();
            }
            foreach (var file in Xml.Root.Elements(xname("file", ns.asmv2)))
            {
                GetFile(file, getUrlFolder(path + "/" + codebase.Replace('\\', '/')), Subfolder ?? version);
            }
            foreach (var fa in Xml.Root.Elements(xname("fileAssociation", ns.cov1)))
            {
                Platform.AssociateFile(fa);
            }
            var entryPoint = Xml.Root.Element(xname("entryPoint", ns.asmv2));
            if (entryPoint != null)
            {
                
                entry.executable = entryPoint.Element(xname("commandLine", ns.asmv2)).Attribute("file").Value;
                entry.folder = new DirectoryInfo(copyto ?? version).FullName; // Alsolute reference.
                entry.version = assemblyIdentity.Attribute("version").Value;
                entry.displayName = entryPoint.Element(xname("assemblyIdentity", ns.asmv2)).Attribute("name").Value;
            }
            var description = Xml.Root.Element(xname("description", ns.asmv1));
            if (description != null)
            {
                string iconFile = description.Attribute(xname("iconFile", ns.asmv2)).Value;
                if (File.Exists(Path.Combine(Subfolder ?? version, iconFile)))
                {
                    entry.icon = Path.Combine(Subfolder ?? version, iconFile);
                }
            }
            if (!String.IsNullOrWhiteSpace(Subfolder))
            {
                if (!File.Exists(Path.Combine(Subfolder, Path.GetFileName(codebase))))
                    File.Copy(Path.Combine(".", version, Path.GetFileName(codebase)), Path.Combine(Subfolder, Path.GetFileName(codebase)));
            }
        }

    }
}

