using PlistCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ClickMac
{
    public static class Platform
    {
        public static readonly bool IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);
        private static string libraryLocation;

        static string InfoPlist { get { return Program.infoPlist; } }

        #region docs
        /*
             <fileAssociation 
                extension=".usbfsp" 
                description="USBNet FileSync Placeholder" 
                progid="usbnet" 
                defaultIcon="Resources\Network-Folder.ico" 
                xmlns="urn:schemas-microsoft-com:clickonce.v1" /> 
             */
        /*		<dict>
        <key>CFBundleTypeExtensions</key>
        <array>
            <string>f4v</string>
            <string>flv</string>
        </array>
        <key>CFBundleTypeIconFile</key>
        <string>flv.icns</string>
        <key>CFBundleTypeName</key>
        <string>Flash Video File</string>
        <key>CFBundleTypeRole</key>
        <string>Viewer</string>
    </dict>*/
        #endregion

        internal static void AssociateFile(XEleDict fa, Manifest application)
        {
            var ext = fa["extension"].TrimStart('.');
            if (File.Exists(InfoPlist)) // OSX is the only one who cares about plists.
                AssociateFileExtMac(fa, ext);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    AssociateFileExtWin32(fa, ext);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Cannot write to Registry.  Elevating.");
                    DoElevate(fa, ext, application);
                }
            }
            AssociateInternal(fa, application);
        }

        private static void DoElevate(XEleDict fa, string ext, Manifest application)
        {
            //TODO [7] : Make this better - We're currently elevating once per extension.
            //File.WriteAllText("assoc.xml", fa.inner.ToString());
            //Process process = Process.Start(new ProcessStartInfo
            //{
            //    FileName = Assembly.GetEntryAssembly().Location,
            //    UseShellExecute = true,
            //    Verb = "runas",
            //    Arguments = String.Format("-associate \"{0}\"", application.Location)
            //});
            //if (process != null)
            //{
            //    process.WaitForExit();
            //}
        }

        private static void AssociateInternal(XEleDict fa, Manifest application)
        {
            Dictionary<string, string> data;
            if (File.Exists("assocs.plist"))
                data = ConvertPlistToStringDict((Dictionary<string, object>)Plist.readPlist("assocs.plist"));
            else
                data = new Dictionary<string, string>();
            data[fa["extension"]] = application.Entry.DeploymentProviderUrl;
            try
            {
                Plist.writeXml(data, "assocs.plist");
            }
            catch (IOException)
            { }
        }

        private static Dictionary<string, string> ConvertPlistToStringDict(Dictionary<string, object> dictionary)
        {
            var ret = new Dictionary<string, string>();
            foreach (var item in dictionary)
            {
                ret.Add(item.Key, item.Value.ToString());
            }
            return ret;
        }
        
        private static void AssociateFileExtWin32(XEleDict fa, string ext)
        {
            ext = "." + ext;
            var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (key == null)
            {
                DoWin32Assoc(fa, ext);
                key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            }
            key = key.OpenSubKey("OpenWithProgIds");
            if (key == null)
                DoWin32Assoc(fa, ext);
            else if (!key.GetValueNames().Contains(fa["progid"]))
                DoWin32Assoc(fa,ext);
            key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(fa["progid"]);
            string[] subs = new string[] { "shell", "open", "command" };
            foreach (var k in subs)
            {
                if (key == null)
                {
                    DoWin32Assoc2(fa);
                    return; 
                }
                key = key.OpenSubKey(k);
            }
            var expected = String.Format("{0} -o \"%1\"", Program.Location);
            if ((string)key.GetValue("", "") != expected)
                DoWin32Assoc2(fa);
        }

        private static void DoWin32Assoc(XEleDict fa, string ext)
        {
            var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(ext);
            key.SetValue("", fa["progid"], Microsoft.Win32.RegistryValueKind.String);
            key = key.CreateSubKey("OpenWithProgids");
            key.SetValue(fa["progid"], "");
        }
        private static void DoWin32Assoc2(XEleDict fa)
        {
            var key = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(fa["progid"]);
            key.SetValue("", fa["description"], Microsoft.Win32.RegistryValueKind.String);
//            key.SetValue("DeploymentProviderUrl", Loading.entry.DeploymentProviderUrl);
            string[] subs = new string[] { "shell", "open", "command" };
            foreach (var k in subs)
                key = key.CreateSubKey(k);
            key.SetValue("", String.Format("{0} -o \"%1\"", Program.Location));
        }

        private static void AssociateFileExtMac(XEleDict fa, string ext)
        {

            Dictionary<string, dynamic> plist = (Dictionary<string, dynamic>)Plist.readPlist(InfoPlist);

            List<Dictionary<string, dynamic>> CFBundleDocumentTypes;
            if (plist.ContainsKey("CFBundleDocumentTypes"))
                CFBundleDocumentTypes = (List<Dictionary<string, dynamic>>)plist["CFBundleDocumentTypes"];
            else
                CFBundleDocumentTypes = new List<Dictionary<string, dynamic>>();
            foreach (var dict in CFBundleDocumentTypes)
            {
                if (!dict.ContainsKey("CFBundleTypeExtensions"))
                    continue; // It's not a FileExtension setting.
                List<string> types = (List<string>)dict["CFBundleTypeExtensions"];
                if (types.Contains(ext))
                {
                    //TODO [8] : Make sure other props [ie: Icon] are correctly set.
                    return;
                }
            }
            var ndict = new Dictionary<string, dynamic>();
            ndict.Add("CFBundleTypeExtensions", new string[] { ext });
            ndict.Add("CFBundleTypeIconFile", Loading.FixFileSeperator(fa["defaultIcon"]));
            ndict.Add("CFBundleTypeName", fa["description"]);

            PlistCS.Plist.writeXml(plist, InfoPlist);
        }

        internal static string GetManifestForExt(string ext)
        {
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                    key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey((string)key.GetValue(""));
                    var manifest = (string)key.GetValue("DeploymentProviderUrl");
                    return manifest;
                }
            }
            catch (Exception) { }
            try
            {
                Dictionary<string, string> data;
            if (File.Exists("assocs.plist"))
                data = ConvertPlistToStringDict((Dictionary<string, object>)Plist.readPlist("assocs.plist"));
            else
                data = new Dictionary<string, string>();
            return data[ext];
            }
            catch (Exception) { }
            return null;
        }

        [Obsolete("We support Online manifests now.  We don't need to lose info like this.")]
        public static string GetLocalManifest(string manifest)
        {
            //var localmanifest = Path.Combine(Path.GetDirectoryName(Program.Location), Path.GetFileName(manifest));
            //if (File.Exists(localmanifest))
            //    return localmanifest;
            return manifest;
        }

        public static OperatingSystem GetPlatform()
        {
            switch (Environment.OSVersion.Platform) {

                case PlatformID.Unix:
                    if (System.IO.Directory.Exists ("/Library"))
                        return new OperatingSystem (PlatformID.MacOSX, Environment.OSVersion.Version);
                    else
                        return Environment.OSVersion;
                case PlatformID.MacOSX: // Wow, they actually got it!
                                        // Not going to ever get called, because of this code:
                    // https://github.com/mono/mono/blob/9e396e4022a4eefbcdeeae1d86c03afbf04043b7/mcs/class/corlib/System/Environment.cs#L239
                case PlatformID.Win32NT:
                default:
                    return Environment.OSVersion;

            }
        }

        public static string LibraryLocation {
            get
            {
                if (string.IsNullOrEmpty(libraryLocation))
                {
                    libraryLocation = Environment.CurrentDirectory; // If all else fails, fall back to Portable Mode.
                    switch (GetPlatform().Platform)
                    {
                        case PlatformID.MacOSX:  // ~/Library/
                            libraryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "ClickOnce");
                            break;
                        case PlatformID.Win32NT: // ~/Appdata/Local
                        case PlatformID.Unix:    // ~/.config/
                            var oldpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClickMac");
                            libraryLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Apps", "ClickOnce");
                            if (Directory.Exists(oldpath))
                                libraryLocation = oldpath;
                            break;
                        default:

                            break;
                    }
                    Directory.CreateDirectory(libraryLocation);
                    Directory.CreateDirectory(Path.Combine(libraryLocation, "Manifests"));
                }
                return libraryLocation;
            }
            set
            {
                libraryLocation = value;
                Directory.CreateDirectory(libraryLocation);
                Directory.CreateDirectory(Path.Combine(libraryLocation, "Manifests"));
            }
        }

        [Obsolete]
        public static string GetLibraryLocation()
        {
            return LibraryLocation;
        }

    }
}
