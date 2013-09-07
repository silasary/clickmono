using Kamahl.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;

namespace ClickMac
{
    internal static class Program
    {
        public static EntryPoint entry = new EntryPoint();
        static Process process;

        static bool InternalLaunch = true;

        public static string infoPlist { get { return Path.Combine("..", "Info.plist"); } }

        private static void Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Location);
            if (args.Length >= 1 && args[0] == "-associate")
            {
                Loading.ReadManifest(Platform.GetLocalManifest(args[1]));
                return;
            }
            else if (args.Length > 1 && args[0] == "-o")
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
                    LoadUnknownFile(args[0]);
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
                        return;
                    }
                }
            }
            if (File.Exists(infoPlist))
            {
                dynamic plist = PlistCS.Plist.readPlist(infoPlist);
                Console.WriteLine("Setting plist icon to '{0}'", entry.icon);
                plist["CFBundleIconFile"] = Loading.FixFileSeperator( entry.icon);
                plist["CFBundleDisplayName"] = entry.displayName;
                PlistCS.Plist.writeXml(plist, infoPlist);
            }
            if (!String.IsNullOrWhiteSpace(entry.executable))
            {
                Environment.SetEnvironmentVariable("ClickOnceAppVersion", entry.version);
                if (Environment.CurrentDirectory == Path.GetDirectoryName(Location))
                    Environment.CurrentDirectory = entry.folder;
                if (InternalLaunch)
                {
                    try
                    {
                        FileInfo targetFile = new FileInfo(Path.Combine(entry.folder, entry.executable));
                        Console.WriteLine("Launching from {0}", targetFile.FullName);
                        // No direct referecnes are ever made between the loader and the API.  
                        // This means applications may use outdated DLLs without the CLR loading two seperate instances
                        // And therefore not communicating properly.
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.Deployed", true);  
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.ActivationUri", new Uri(entry.DeploymentProviderUrl));
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.Version", new Version(entry.version));
                        var Target = Assembly.LoadFile(targetFile.FullName);
                        if (Target.EntryPoint.GetParameters().Length == 0)
                            Target.EntryPoint.Invoke(null, null);
                        else
                            Target.EntryPoint.Invoke(null, new string[][] { args });
                    }
                    catch (Exception v)
                    {
                        GC.Collect();
                        Console.WriteLine(v.ToString());
                        Launch(args);
                        Console.CancelKeyPress += Console_CancelKeyPress;
                        process.WaitForExit();
                    }
                }
                else
                {
                    Launch(args);
                    Console.CancelKeyPress += Console_CancelKeyPress;
                    process.WaitForExit();
                }
            }
            if (CheckForSelfUpdate(null))
                return;
        }

        private static bool GetManifestFromName(string p)
        {
            Dictionary<string, string> manifests = null;
            try
            {
                manifests = Serialization.LoadFromJson<Dictionary<string, string>>(new WebClient().DownloadString("https://dl.dropboxusercontent.com/u/4187827/ClickOnce/manifests.txt"));
            }
            catch (WebException) { return false; }
            if (!manifests.ContainsKey(p))
                return false;
            Loading.ReadManifest(manifests[p]);
            return true;
        }

        private static void Launch(string[] args)
        {
            try
            {
                process = Process.Start(new ProcessStartInfo(Path.Combine(entry.folder, entry.executable), String.Join(" ", args)) { RedirectStandardOutput = true, UseShellExecute = false });
                process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                process.BeginOutputReadLine();
            }
            catch (Win32Exception)
            {
                try
                {
                    process = Process.Start(new ProcessStartInfo("mono", String.Format("{0} {1}", Path.Combine(entry.folder, entry.executable), String.Join(" ", args))) { RedirectStandardOutput = true, UseShellExecute = false });
                    process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                    process.BeginOutputReadLine();
                }
                catch (Win32Exception)
                {
                    try
                    {
                        process = Process.Start(new ProcessStartInfo("mono", Path.Combine(entry.folder, entry.executable)) { UseShellExecute = true });
                    }
                    catch (Win32Exception)
                    {
                        process = Process.Start(new ProcessStartInfo(Path.Combine(entry.folder, entry.executable)) { UseShellExecute = true });
                    }
                }
            }
        }

        private static bool CheckForSelfUpdate(string[] args)
        {
            if (Debugger.IsAttached && args != null)
                return false;
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                if (File.Exists("ClickMac.old.exe"))
                    File.Delete("ClickMac.old.exe");
                if (File.Exists("Kamahl.Deployment.dll.old"))
                    File.Delete("Kamahl.Deployment.dll.old");
                if (!File.Exists("ClickMac.application"))
                    File.WriteAllText("ClickMac.application", "<?xml version=\"1.0\" encoding=\"utf-8\"?><asmv1:assembly xsi:schemaLocation=\"urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd\" manifestVersion=\"1.0\" xmlns:asmv1=\"urn:schemas-microsoft-com:asm.v1\" xmlns=\"urn:schemas-microsoft-com:asm.v2\" xmlns:asmv2=\"urn:schemas-microsoft-com:asm.v2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ><assemblyIdentity name=\"ClickMac.application\" version=\"1.0.0.0\" publicKeyToken=\"3ff3731db18bf4c7\" language=\"neutral\" processorArchitecture=\"msil\" xmlns=\"urn:schemas-microsoft-com:asm.v1\" /><description asmv2:publisher=\"ClickMac\" asmv2:product=\"ClickMac\" xmlns=\"urn:schemas-microsoft-com:asm.v1\" /><deployment install=\"true\" mapFileExtensions=\"true\"><subscription><update><beforeApplicationStartup /></update></subscription><deploymentProvider codebase=\"https://dl.dropbox.com/u/4187827/ClickOnce/ClickMac.application\" /></deployment></asmv1:assembly>");
                entry = new EntryPoint();
                Loading.ReadManifest("ClickMac.application");
                if (entry.executable == null)
                    return false;
                if (!File.Exists("ClickMac.version") || entry.version != File.ReadAllText("ClickMac.version"))
                {
                    var loc = Assembly.GetExecutingAssembly().Location;
                    var deploc = Path.Combine(Path.GetDirectoryName(loc), "Kamahl.Deployment.dll");
                    File.Move(loc, "ClickMac.old.exe");
                    if (File.Exists(deploc))
                        File.Move(deploc, "Kamahl.Deployment.dll.old");
                    File.Copy(Path.Combine(entry.folder, entry.executable), loc);
                    if (File.Exists(Path.Combine(entry.folder, "Kamahl.Deployment.dll")))
                        File.Copy(Path.Combine(entry.folder, "Kamahl.Deployment.dll"), deploc);
                    File.WriteAllText("ClickMac.version", entry.version);
                    Console.WriteLine("Updated {0} to version {1}", Path.GetFileName(loc), entry.version);
                    if (args != null)
                    {
                        var Updated = Assembly.LoadFile(loc);
                        Updated.EntryPoint.Invoke(null, new string[][] { args });
                        return true;
                    }
                }
                entry = new EntryPoint();
            }
            catch (WebException)
            { }
            catch (IOException) 
            { } // Chances are, we just updated, and clickmac.old.exe is still running.
            return false;

        }

        private static void LoadUnknownFile(string file)
        {
            var ext = Path.GetExtension(file);
            Loading.ReadManifest(Platform.GetManifestForExt(ext));
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (process != null)
            {
                process.CloseMainWindow();
            }
        }




        public class EntryPoint
        {
            public string DeploymentProviderUrl;

            public string executable;
            public string folder;
            public string version;
            public string icon;
            public string displayName;

        }

        public static string Location
        {
            get
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }
    }
}