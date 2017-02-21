using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace ClickMac
{
    internal static class Loading
    {
        public delegate void log(string s, params object[] args);
        public static log Log;
        public static bool PortableMode = false;

        public static string FixFileSeperator(string path)
        {
            if (String.IsNullOrEmpty(path))
                return path;
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }


        public static Manifest LoadApplicationManifest(string manifestUri)
        {
            Log("Loading {0}", manifestUri);
            var wd = Environment.CurrentDirectory;
            if (!Loading.PortableMode)
                Environment.CurrentDirectory = Platform.GetLibraryLocation( );

            DeploymentOptions options = new DeploymentOptions();

            var application = new Manifest(manifestUri, options);
            application.ProcessDependencies();

            Environment.CurrentDirectory = wd;
            return application;
        }
    }
}
