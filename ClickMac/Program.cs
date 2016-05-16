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

        static bool InternalLaunch = false;

        public static string infoPlist { get { return Path.Combine("..", "Info.plist"); } }

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Loading.Log = Console.WriteLine;
            Environment.CurrentDirectory = Path.GetDirectoryName(Location);
            Console.WriteLine("Running on {0}", Platform.GetPlatform( ));
            if (PreLoading.DoArgs(ref args) == true)
                return;
            if (File.Exists(infoPlist))
            {
                dynamic plist = PlistCS.Plist.readPlist(infoPlist);
                Console.WriteLine("Setting plist icon to '{0}'", Loading.entry.icon);  // If not in Portable Mode, this points to a file somewhere in /Users/Me/Library/ClickOnce/*.ico - This is not Ideal.
                plist["CFBundleIconFile"] = Loading.FixFileSeperator(Loading.entry.icon);  // TODO: Check relative Path, and copy Icon into App Bundle if needed. Of course, all of this assumes running on a Mac.
                plist["CFBundleDisplayName"] = Loading.entry.displayName;                  // PCs will just use the embedded EXE Icon, or not care in the slightest.  Also, They'll probably just end up using 
                PlistCS.Plist.writeXml(plist, infoPlist);                                  // The Official Clickonce implementation, unless they're running on 9x, and need Mono+ClickMac.
            }                                                                              // What do you mean I'm the only person that's ever going to apply to?  But yeah, 9x people can deal with the COMMAND.COM icon.
            if (!String.IsNullOrWhiteSpace(Loading.entry.executable))
            {
                Environment.SetEnvironmentVariable("ClickOnceAppVersion", Loading.entry.version);
                if (Environment.CurrentDirectory == Path.GetDirectoryName(Location))
                    Environment.CurrentDirectory = Loading.entry.folder;
                #if AppDomains
                if (InternalLaunch)
                {
                    Console.WriteLine("Warning!  Using depreciated Internal Launch code");
                    #region InternalLaunchCode
                    var launchTime = DateTime.Now;
                    try
                    {
                        FileInfo targetFile = new FileInfo(Loading.entry.executable);
                        Console.WriteLine("Launching from {0}", targetFile.FullName);
                        // No direct references are ever made between the loader and the API.  
                        // This means applications may use outdated DLLs without the CLR loading two seperate instances
                        // And therefore not communicating properly.
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.Deployed", true);
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.ActivationUri", new Uri(Loading.entry.DeploymentProviderUrl));
                        AppDomain.CurrentDomain.SetData("Kamahl.Deployment.Version", new Version(Loading.entry.version));
                        var Target = Assembly.LoadFile(targetFile.FullName);
                        if (Target.EntryPoint.GetParameters().Length == 0)
                            Target.EntryPoint.Invoke(null, null);
                        else
                            Target.EntryPoint.Invoke(null, new string[][] { args });
                    }
                    catch (Exception v)
                    {
                        GC.Collect();  // Why were we doing this here?
                                       // Not worth figuring out because we've depreciated the whole thing anyway.
                        Console.WriteLine(v.ToString());
                        if (DateTime.Now.Subtract(launchTime).TotalSeconds < 10)
                        {
                            Launch(args);
                            Console.CancelKeyPress += Console_CancelKeyPress;
                            process.WaitForExit();
                        }
                    }
                    #endregion
                }
                else
                {
                #endif
                    Launch(args);
                    Console.CancelKeyPress += Console_CancelKeyPress;
                    process.WaitForExit();
                #if AppDomains
                }
                #endif
            }
            if (CheckForSelfUpdate(null))
                return;
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Failed to Load '{0}'", args.Name);
            return null;
        }




        private static void Launch(string[] args)
        {
            //Console.WriteLine("Launching '{0}' from {1}", Loading.entry.executable, Environment.CurrentDirectory);
            try
            {
                process = Process.Start(new ProcessStartInfo(Loading.entry.executable, String.Join(" ", args)) { RedirectStandardOutput = true, UseShellExecute = false });
                process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                process.BeginOutputReadLine();
            }
            catch (Win32Exception)
            {
                try
                {
                    process = Process.Start(new ProcessStartInfo("mono", String.Format("{0} {1}", Loading.entry.executable, String.Join(" ", args))) { RedirectStandardOutput = true, UseShellExecute = false });
                    process.OutputDataReceived += new DataReceivedEventHandler((o, e) => { Console.WriteLine(e.Data); });
                    process.BeginOutputReadLine();
                }
                catch (Win32Exception)
                {
                    try
                    {
                        process = Process.Start(new ProcessStartInfo("mono", Loading.entry.executable) { UseShellExecute = true });
                    }
                    catch (Win32Exception)
                    {
                        process = Process.Start(new ProcessStartInfo(Loading.entry.executable) { UseShellExecute = true });
                    }
                }
            }
        }

        private static bool CheckForSelfUpdate(string[] args)
        {
            #if DEBUG
            if (Debugger.IsAttached && args != null)
                return false;
            #endif
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                if (File.Exists("ClickMac.old.exe"))
                    File.Delete("ClickMac.old.exe");
                if (File.Exists("Kamahl.Deployment.dll.old"))
                    File.Delete("Kamahl.Deployment.dll.old");
                if (!File.Exists("ClickMac.application"))
                    File.WriteAllText("ClickMac.application", "<?xml version=\"1.0\" encoding=\"utf-8\"?><asmv1:assembly xsi:schemaLocation=\"urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd\" manifestVersion=\"1.0\" xmlns:asmv1=\"urn:schemas-microsoft-com:asm.v1\" xmlns=\"urn:schemas-microsoft-com:asm.v2\" xmlns:asmv2=\"urn:schemas-microsoft-com:asm.v2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ><assemblyIdentity name=\"ClickMac.application\" version=\"1.0.0.0\" publicKeyToken=\"3ff3731db18bf4c7\" language=\"neutral\" processorArchitecture=\"msil\" xmlns=\"urn:schemas-microsoft-com:asm.v1\" /><description asmv2:publisher=\"ClickMac\" asmv2:product=\"ClickMac\" xmlns=\"urn:schemas-microsoft-com:asm.v1\" /><deployment install=\"true\" mapFileExtensions=\"true\"><subscription><update><beforeApplicationStartup /></update></subscription><deploymentProvider codebase=\"https://dl.dropbox.com/u/4187827/ClickOnce/ClickMac.application\" /></deployment></asmv1:assembly>");
                Loading.entry = new Loading.EntryPoint();
                Loading.ReadManifest("ClickMac.application");
                if (Loading.entry.executable == null)
                    return false;
                if (!File.Exists("ClickMac.version") || Loading.entry.version != File.ReadAllText("ClickMac.version"))
                {
                    var loc = Assembly.GetExecutingAssembly().Location;
                    var deploc = Path.Combine(Path.GetDirectoryName(loc), "Kamahl.Deployment.dll");
                    File.Move(loc, "ClickMac.old.exe");
                    if (File.Exists(deploc))
                        File.Move(deploc, "Kamahl.Deployment.dll.old");
                    File.Copy(Path.Combine(Loading.entry.folder, Loading.entry.executable), loc);
                    if (File.Exists(Path.Combine(Loading.entry.folder, "Kamahl.Deployment.dll")))
                        File.Copy(Path.Combine(Loading.entry.folder, "Kamahl.Deployment.dll"), deploc);
                    File.WriteAllText("ClickMac.version", Loading.entry.version);
                    Console.WriteLine("Updated {0} to version {1}", Path.GetFileName(loc), Loading.entry.version);
                    if (args != null)
                    {
                        var Updated = Assembly.LoadFile(loc);
                        Updated.EntryPoint.Invoke(null, new string[][] { args });
                        return true;
                    }
                }
                Loading.entry = new Loading.EntryPoint();
            }
            catch (WebException)
            { }
            catch (IOException) 
            { } // Chances are, we just updated, and clickmac.old.exe is still running.
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
    }
}