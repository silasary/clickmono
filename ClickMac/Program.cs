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
    internal class Program
    {
        public static EntryPoint entry = new EntryPoint();
        static Process process;

        public static string infoPlist { get { return Path.Combine("..", "Info.plist"); } }

        private static void Main(string[] args)
        {
            if (args.Length > 1 && args[0] == "-o")
            {
                LoadUnknownFile(args[1]);
                args = args.Skip(1).ToArray();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
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
                if (manifests.Length >= 1)
                    Loading.ReadManifest(manifests[0]);
            }
            if (File.Exists(infoPlist ))
            {
                dynamic plist = PlistCS.Plist.readPlist(infoPlist);
                Console.WriteLine("Setting plist icon to '{0}'", entry.icon);
                plist["CFBundleIconFile"] = Loading.FixFileSeperator( entry.icon);
                plist["CFBundleDisplayName"] = entry.displayName;
                PlistCS.Plist.writeXml(plist, infoPlist);
            }
            if (!String.IsNullOrWhiteSpace(entry.executable))
            {
                //var name = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("System.Deployment.dll"));
                //using (System.IO.Stream manifestResourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                //{
                //    byte[] array = new byte[manifestResourceStream.Length];
                //    manifestResourceStream.Read(array, 0, array.Length);
                //    File.WriteAllBytes(Path.Combine(entry.folder, "System.Deployment.dll"), array);
                //}
                Environment.SetEnvironmentVariable("ClickOnceAppVersion", entry.version);
                
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
                Console.CancelKeyPress += Console_CancelKeyPress;
                process.WaitForExit();
            }
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