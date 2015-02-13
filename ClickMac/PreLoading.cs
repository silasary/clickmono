//using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace ClickMac
{
    class PreLoading
    {
        internal static bool DoArgs(ref string[] args)
        {
            if (args.Length >= 1 && args[0] == "-associate") // Called by Platform.DoElevate().
            {
                Loading.ReadManifest(Platform.GetLocalManifest(args[1]));
                return true; // Abort - We can't accidentally run the app with Elevated Permissions.
            }
            else if (args.Length > 1 && args[0] == "-o")  // Called by Explorer, when the user double-clicks a file.
            {
                args[1] = new FileInfo(args[1]).FullName; // This is stupid and redudant.  
                // But it stops windows throwing around stupid 8.3 names, which break EVERYTHING! :/
                // RANT: Why the hell did Windows 8 even give me an 8.3 name in the first place?
                LoadUnknownFile(args[1]);
                args = args.Skip(1).ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(' '))
                        args[i] = String.Format("\"{0}\"", args[i]);
                }
            }
            else if (args.Length > 0 && File.Exists(args[0]))
            {
                if (Path.GetExtension(args[0]).ToLower() != ".application" && Path.GetExtension(args[0]).ToLower() != ".manifest")
                {
                    LoadUnknownFile(args[0]);  // Associated file.  Or they're screwing with us. 
                }
                else
                {
                    Loading.ReadManifest(args[0]);
                    args = args.Skip(1).ToArray();
                }
            }
            else
            {
                var manifests = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.application");
                var pref = manifests.FirstOrDefault(x => !Path.GetFileName(x).StartsWith("ClickMac"));
                if (pref != null)
                    Loading.ReadManifest(pref);
                else
                {
                    if (!GetManifestFromName(Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)))
                    {
                        Console.WriteLine("Could not find an .application file.  Aborting.");
                        return true;
                    }
                }
            }
            return false;
        }

        private static void LoadUnknownFile(string file)
        {
            var ext = Path.GetExtension(file);
            Loading.ReadManifest(Platform.GetManifestForExt(ext));
        }

        public static bool GetManifestFromName(string p)
        {
            Dictionary<string, string> manifests = null;
            try
            {
                return false;
                //manifests = Serialization.LoadFromJson<Dictionary<string, string>>(new WebClient().DownloadString("https://dl.dropboxusercontent.com/u/4187827/ClickOnce/manifests.txt"));
            }
            catch (WebException) { return false; }
            if (!manifests.ContainsKey(p))
                return false;
            Loading.ReadManifest(manifests[p]);
            return true;
        }

    }
}
