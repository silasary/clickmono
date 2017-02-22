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
        static Process process;

        public static string infoPlist { get { return Path.Combine("..", "Info.plist"); } }

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Loading.Log = Console.WriteLine;
            InvokationDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetDirectoryName(Location);
            Console.WriteLine("Running on {0}", Platform.GetPlatform( ));
            var application = PreLoading.DoArgs(ref args);
            if (application == null)
            {
                CheckForSelfUpdate(null);
                return;
            }

            if (File.Exists(infoPlist))
            {
                dynamic plist = PlistCS.Plist.readPlist(infoPlist);
                Console.WriteLine("Setting plist icon to '{0}'", application.Entry.icon);  // If not in Portable Mode, this points to a file somewhere in /Users/Me/Library/ClickOnce/*.ico - This is not Ideal.
                plist["CFBundleIconFile"] = Loading.FixFileSeperator(application.Entry.icon);  // TODO: Check relative Path, and copy Icon into App Bundle if needed. Of course, all of this assumes running on a Mac.
                plist["CFBundleDisplayName"] = application.Entry.displayName;                  // PCs will just use the embedded EXE Icon, or not care in the slightest.  Also, They'll probably just end up using 
                PlistCS.Plist.writeXml(plist, infoPlist);                                  // The Official Clickonce implementation, unless they're running on 9x, and need Mono+ClickMac.
            }                                                                              // What do you mean I'm the only person that's ever going to apply to?  But yeah, 9x people can deal with the COMMAND.COM icon.
            if (!String.IsNullOrWhiteSpace(application.Entry.executable))
            {
                Environment.SetEnvironmentVariable("ClickOnceAppVersion", application.Entry.version);
                Environment.SetEnvironmentVariable("ClickOncePid", Process.GetCurrentProcess().Id.ToString());

                Launch(application, args);
                Console.CancelKeyPress += Console_CancelKeyPress;
                process.WaitForExit();
            }
            if (CheckForSelfUpdate(null))
                return;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Failed to Load '{0}'", args.Name);
            return null;
        }

        private static void Launch(Manifest application, string[] args)
        {
            string executable = Path.Combine(application.Entry.folder, application.Entry.executable);
            Console.WriteLine("Launching '{0}' in {1}", executable, Environment.CurrentDirectory);
            try
            {
                process = Process.Start(new ProcessStartInfo(executable, string.Join(" ", args)) { RedirectStandardOutput = true, UseShellExecute = false });
                process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                process.BeginOutputReadLine();
            }
            catch (Win32Exception)
            {
                try
                {
                    process = Process.Start(new ProcessStartInfo("mono", string.Format("{0} {1}", executable, string.Join(" ", args))) { RedirectStandardOutput = true, UseShellExecute = false });
                    process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                    process.BeginOutputReadLine();
                }
                catch (Win32Exception)
                {
                    try
                    {
                        process = Process.Start(new ProcessStartInfo("mono", executable) { UseShellExecute = true });
                    }
                    catch (Win32Exception)
                    {
                        process = Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
                    }
                }
            }
        }

        private static bool CheckForSelfUpdate(string[] args)
        {
#if DEBUG
             return false;
#endif
#pragma warning disable CS0162 // Unreachable code detected
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#pragma warning restore CS0162 // Unreachable code detected
            try
            {
                if (File.Exists("ClickMac.old.exe"))
                    File.Delete("ClickMac.old.exe");
                if (File.Exists("Kamahl.Deployment.dll.old"))
                    File.Delete("Kamahl.Deployment.dll.old");
                var update = Loading.LoadApplicationManifest("https://katelyngigante.com/deployment/clickmono/ClickMac.application");
                if (update.Entry.executable == null)
                    return false;
                if (!File.Exists("ClickMac.version") || update.Entry.version != File.ReadAllText("ClickMac.version"))
                {
                    var loc = Assembly.GetExecutingAssembly().Location;
                    var deploc = Path.Combine(Path.GetDirectoryName(loc), "Kamahl.Deployment.dll");
                    File.Move(loc, "ClickMac.old.exe");
                    if (File.Exists(deploc))
                        File.Move(deploc, "Kamahl.Deployment.dll.old");
                    File.Copy(Path.Combine(update.Entry.folder, update.Entry.executable), loc);
                    if (File.Exists(Path.Combine(update.Entry.folder, "Kamahl.Deployment.dll")))
                        File.Copy(Path.Combine(update.Entry.folder, "Kamahl.Deployment.dll"), deploc);
                    File.WriteAllText("ClickMac.version", update.Entry.version);
                    Console.WriteLine("Updated {0} to version {1}", Path.GetFileName(loc), update.Entry.version);
                    if (args != null)
                    {
                        var Updated = Assembly.LoadFile(loc);
                        Updated.EntryPoint.Invoke(null, new string[][] { args });
                        return true;
                    }
                }
            }
            catch (WebException)
            { }
            catch (IOException) 
            { } // Chances are, we just updated, and clickmac.old.exe is still running.
            catch (Exception c)
            {
                Console.WriteLine("Failed Updating: {0}", c);
            }
            return false;

        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (process != null)
            {
                process.CloseMainWindow();
            }
        }

        public static string Location
        {
            get
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        public static string InvokationDirectory { get; private set; }
    }
}