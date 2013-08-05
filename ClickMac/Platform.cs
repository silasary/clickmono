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
    class Platform
    {
        static string infoPlist { get { return Program.infoPlist; } }

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

        public static void AssociateFile(XEleDict fa)
        {
            var ext = fa["extension"].TrimStart('.');
            if (File.Exists(infoPlist)) // OSX is the only one who cares about plists.
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
                    DoElevate(fa, ext);
                }
            }
            AssociateInternal(fa);
        }

        private static void DoElevate(XEleDict fa, string ext)
        {
            File.WriteAllText("assoc.xml", fa.inner.ToString());
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = Assembly.GetEntryAssembly().Location,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = String.Format("-associate {0}", Program.entry.DeploymentProviderUrl)
            });
            if (process != null)
            {
                process.WaitForExit();
            }
        }

        private static void AssociateInternal(XEleDict fa)
        {
            Dictionary<string, string> data;
            if (File.Exists("assocs.plist"))
                data = ConvertPlistToStringDict((Dictionary<string, object>)Plist.readPlist("assocs.plist"));
            else
                data = new Dictionary<string, string>();
            data[fa["extension"]] = Program.entry.DeploymentProviderUrl;
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
            key.SetValue("DeploymentProviderUrl", Program.entry.DeploymentProviderUrl);
            string[] subs = new string[] { "shell", "open", "command" };
            foreach (var k in subs)
                key = key.CreateSubKey(k);
            key.SetValue("", String.Format("{0} -o \"%1\"", Program.Location));
        }

        private static void AssociateFileExtMac(XEleDict fa, string ext)
        {

            Dictionary<string, dynamic> plist = (Dictionary<string, dynamic>)Plist.readPlist(infoPlist);

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
                    //TODO: Make sure other props [ie: Icon] are correctly set.
                    return;
                }
            }
            var ndict = new Dictionary<string, dynamic>();
            ndict.Add("CFBundleTypeExtensions", new string[] { ext });
            ndict.Add("CFBundleTypeIconFile", Loading.FixFileSeperator(fa["defaultIcon"]));
            ndict.Add("CFBundleTypeName", fa["description"]);

            PlistCS.Plist.writeXml(plist, infoPlist);
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
                    return GetLocalManifest(manifest);
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
            return GetLocalManifest(data[ext]);
            }
            catch (Exception) { }
            return null;
        }

        public static string GetLocalManifest(string manifest)
        {
            var localmanifest = Path.Combine(Path.GetDirectoryName(Program.Location), Path.GetFileName(manifest));
            if (File.Exists(localmanifest))
                return localmanifest;
            return manifest;
        }
    }
}
