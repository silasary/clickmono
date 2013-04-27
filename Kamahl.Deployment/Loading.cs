using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Kamahl.Deployment
{
    class Loading
    {
        private HostingManager Manager;
        
        public Loading(HostingManager manager)
        {
            this.Manager = manager;
        }
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
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }


        public void ReadManifest(string manifest)
        {
            var doc = XDocument.Load(manifest);
            var deployment = doc.Root.Element(xname("deployment", ns.asmv2));
            var provider = deployment.Element(xname("deploymentProvider", ns.asmv2)).Attribute("codebase").Value;
            
            XDocument newManifest = null;
            try
            {
                Manager.Info("Getting manifest from {0}", provider);
                newManifest = XDocument.Load(new WebClient().DownloadString(provider));
                newManifest.Save(newManifest.Root.Element(xname("assemblyIdentity", ns.asmv1)).Attribute("name").Value);
            }
            catch (WebException)
            {
                Manager.Info("Getting manifest failed. Starting in Offline Mode");
                newManifest = XDocument.Load(manifest);
            }
            LoadManifest(newManifest);
        }

        private static void LoadManifest(XDocument manifest)
        {
            var deployment = manifest.Root.Element(xname("deployment", ns.asmv2));
            string path = getUrlFolder(deployment.Element(xname("deploymentProvider", ns.asmv2)).Attribute("codebase").Value);
            foreach (var dependency in manifest.Root.Elements(xname("dependency", ns.asmv2)))
            {
                ProcessDependancy(dependency, path, null);
            }
        }

        private static void ProcessDependancy(XElement dependency, string path, string copyto)
        {
            var dependentAssembly = dependency.Element(xname("dependentAssembly", ns.asmv2));
            if (dependentAssembly == null || dependentAssembly.Attribute("dependencyType").Value != "install")
                return;
            var codebase = dependentAssembly.Attribute("codebase").Value;
            var assemblyIdentity = dependentAssembly.Element(xname("assemblyIdentity", ns.asmv2));
            string version = String.Format("{0}_{1}", assemblyIdentity.Attribute("name").Value, assemblyIdentity.Attribute("version").Value);
            Directory.CreateDirectory(version);
            string filename = Path.Combine(version, Path.GetFileName(codebase));
            bool downloaded = false;
            if (File.Exists(filename))
            {
                if (new FileInfo(filename).Length == int.Parse(dependentAssembly.Attribute("size").Value))
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
                    Console.WriteLine("Getting Dependancy {0}", codebase);
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
                            Console.WriteLine("\tFailed to download!  Application might not work.");
                            return;
                        }
                    }
                }
            }
            if (File.Exists(filename + "._"))
                File.Delete(filename + "._");
            if (Path.GetExtension(codebase) == ".manifest")
            {
                var manifest = XDocument.Load(Path.Combine(".", version, Path.GetFileName(codebase)));
                foreach (var dependancy in manifest.Root.Elements(xname("dependency", ns.asmv2)))
                {
                    ProcessDependancy(dependancy, getUrlFolder(path + "/" + codebase.Replace('\\', '/')), copyto ?? version);
                }
                foreach (var file in manifest.Root.Elements(xname("file", ns.asmv2)))
                {
                    GetFile(file, getUrlFolder(path + "/" + codebase.Replace('\\', '/')), copyto ?? version);
                }
                foreach (var fa in manifest.Root.Elements(xname("fileAssociation", ns.cov1)))
                {
                    Platform.AssociateFile(fa);
                }
                var entryPoint = manifest.Root.Element(xname("entryPoint", ns.asmv2));
                if (entryPoint != null)
                {
                    
                       Program.entry.executable = entryPoint.Element(xname("commandLine", ns.asmv2)).Attribute("file").Value;
                       Program.entry.folder = copyto ?? version;
                       Program.entry.version = assemblyIdentity.Attribute("version").Value;
                       Program.entry.displayName = entryPoint.Element(xname("assemblyIdentity", ns.asmv2)).Attribute("name").Value;
                }
                var description = manifest.Root.Element(xname("description", ns.asmv1));
                if (description != null)
                {
                    string iconFile = description.Attribute(xname("iconFile", ns.asmv2)).Value;
                    if (File.Exists(Path.Combine(copyto ?? version, iconFile)))
                    {
                        Program.entry.icon = Path.Combine(copyto ?? version, iconFile);
                    }
                }

            }
            if (!String.IsNullOrWhiteSpace(copyto))
            {
                if (!File.Exists(Path.Combine(copyto, Path.GetFileName(codebase))))
                    File.Copy(Path.Combine(".", version, Path.GetFileName(codebase)), Path.Combine(copyto, Path.GetFileName(codebase)));
            }
        }
        private static void GetFile(XElement file, string path, string copyto)
        {

            string name = file.Attribute("name").Value;
            string filename = Path.Combine(copyto, name.Replace('\\', Path.DirectorySeparatorChar));
            Console.WriteLine("Getting {0}", filename);
            bool downloaded = false;
            if (File.Exists(filename))
            {
                if (new FileInfo(filename).Length == int.Parse(file.Attribute("size").Value))
                {
                    downloaded = true;
                }
                else
                    File.Delete(filename);
            }
            if (!downloaded)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                try
                {
                    new WebClient().DownloadFile(path + "/" + name.Replace('\\', '/'), filename);
                }
                catch (WebException)
                {
                    new WebClient().DownloadFile(path + "/" + name.Replace('\\', '/') + ".deploy", filename);
                }
            }
        }


        //public static Program.EntryPoint entry { get { return Program.entry; } set { Program.entry = value; } }
    }
}
