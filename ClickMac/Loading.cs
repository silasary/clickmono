using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace ClickMac
{
    static class Loading
    {
        public delegate void log(string s, params object[] args);
        public static log Log;
        public static EntryPoint entry = new EntryPoint();
        public static bool PortableMode = false;

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


        public static void LoadApplicationManifest(string manifestUri) // Entry Point.  Should never be called from within this class.
        {
            Console.WriteLine("Loading {0}", manifestUri);
            var wd = Environment.CurrentDirectory;
            if (!Loading.PortableMode)
                Environment.CurrentDirectory = Platform.GetLibraryLocation( );

            var application = new Manifest(manifestUri);
            LoadManifest(application);
            //entry = application.entry;

            Environment.CurrentDirectory = wd;
        }

        private static void LoadManifest(Manifest manifest)
        {
            //var deployment = manifest.Xml.Root.Element(xname("deployment", ns.asmv2));
            // Check if manifest has been redirected/updated by the DeploymentProvider element
            //string path;
            //if (deployment.Element(xname("deploymentProvider", ns.asmv2)) == null)
            //    path = getUrlFolder(manifest.Location);
            //else
            //    path = getUrlFolder(deployment.Element(xname("deploymentProvider", ns.asmv2)).Attribute("codebase").Value);
            //foreach (var dependency in manifest.Xml.Root.Elements(xname("dependency", ns.asmv2)))
            //{
            //    ProcessDependency(dependency, path, null);
            //}
            manifest.ProcessDependencies();
        }

        internal static void GetFile(XElement file, string path, string copyto)
        {

            string name = file.Attribute("name").Value;
            string filename = Path.Combine(copyto, name.Replace('\\', Path.DirectorySeparatorChar));
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
                            Log("\tFailed to download!  Application might not work.");
                            return;
                        }
                    }
                }
                if (File.Exists(filename + "._"))
                    File.Delete(filename + "._");
            }
        }

       


        //public static Program.EntryPoint entry { get { return Program.entry; } set { Program.entry = value; } }
    }
}
