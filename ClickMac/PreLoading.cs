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
        internal static Manifest DoArgs(ref string[] args)
        {
            Manifest res = null;
            if (args.Length > 1 && args[0] == "-associate") // Called by Platform.DoElevate().
            {
                res = Loading.LoadApplicationManifest(Platform.GetLocalManifest(args[1]));
                return null; // Abort - We can't accidentally run the app with Elevated Permissions.
            }
            else if (args.Length > 0 && args[0] == "-associate")
            {
                Console.WriteLine("ERROR: No manifest provided.");
            }
            else if (args.Length > 0 && args[0] == "--packager")
            {
                res = Loading.LoadApplicationManifest(@"http://ci.katelyngigante.com/job/silasary/job/clickmono/job/master/lastSuccessfulBuild/artifact/Packager/bin/Release/_publish/Packager.exe.application");
                args = args.Skip(1).ToArray();
                Environment.CurrentDirectory = Program.InvokationDirectory;
                return res;
            }
            else if (args.Length > 1 && args[0] == "-o")  // Called by Explorer, when the user double-clicks a file.
            {
                args[1] = new FileInfo(args[1]).FullName; // This is stupid and redudant.  
                // But it stops windows throwing around stupid 8.3 names, which break EVERYTHING! :/
                // RANT: Why the hell did Windows 8 even give me an 8.3 name in the first place?
                res = LoadUnknownFile(args[1]);
                args = args.Skip(1).ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(' '))
                        args[i] = String.Format("\"{0}\"", args[i]);
                }
            }
            else if (args.Length > 0 && File.Exists(args[0]))
            {
                if (Path.GetExtension(args[0]).ToLower() == ".appref-ms")
                {
                    var uri = File.ReadAllText(args[0]); // Untested
                    res = Loading.LoadApplicationManifest(uri);
                    args = args.Skip(1).ToArray();
                }
                else if (Path.GetExtension(args[0]).ToLower() != ".application" && Path.GetExtension(args[0]).ToLower() != ".manifest")
                {
                    res = LoadUnknownFile(args[0]);  // Associated file.  Or they're screwing with us. 
                }
                else
                {
                    res = Loading.LoadApplicationManifest(args[0]);
                    args = args.Skip(1).ToArray();
                }
            }
            else if (args.Length > 0 && Uri.IsWellFormedUriString(args[0], UriKind.Absolute))
            {
                res = Loading.LoadApplicationManifest(args[0]);
                args = args.Skip(1).ToArray();
            }

            else
            {
                var manifests = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.application");
                var pref = manifests.FirstOrDefault(x => !Path.GetFileName(x).StartsWith("ClickMac"));
                if (pref != null)
                    return res = Loading.LoadApplicationManifest(pref);

                manifests = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.appref-ms");
                pref = manifests.FirstOrDefault();
                if (pref != null)
                    return res = Loading.LoadApplicationManifest(File.ReadAllText(pref));

                res = GetManifestFromName(Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
                if (res == null)
                {
                    Console.WriteLine("Could not find an .application file.  Aborting.");
                    return null;
                }
            }
            return res;
        }

        private static Manifest LoadUnknownFile(string file)
        {
            var ext = Path.GetExtension(file);
            return Loading.LoadApplicationManifest(Platform.GetManifestForExt(ext));
        }

        // This is probably problematic. Let's not do it.  (Although maybe I might make yet another software package manager, and that changes?)
        public static Manifest GetManifestFromName(string p)
        {
            return null;
//            Dictionary<string, string> manifests = null;
//            try
//            {
//                manifests = Serialization.LoadFromJson<Dictionary<string, string>>(new WebClient().DownloadString("https://dl.dropboxusercontent.com/u/4187827/ClickOnce/manifests.txt"));
//            }
//            catch (WebException) { return null; }
//            if (!manifests.ContainsKey(p))
//                return null;
//            return Loading.LoadApplicationManifest(manifests[p]);
        }

    }
}
