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
        public static bool PortableMode = false;

        public static string FixFileSeperator(string path)
        {
            if (String.IsNullOrEmpty(path))
                return path;
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }


        public static Manifest LoadApplicationManifest(string manifestUri)
        {
            Console.WriteLine("Loading {0}", manifestUri);
            var wd = Environment.CurrentDirectory;
            if (!Loading.PortableMode)
                Environment.CurrentDirectory = Platform.GetLibraryLocation( );

            var application = new Manifest(manifestUri);
            application.ProcessDependencies();
            //entry = application.entry;

            Environment.CurrentDirectory = wd;
            return application;
        }

        //public static Program.EntryPoint entry { get { return Program.entry; } set { Program.entry = value; } }
    }
}
