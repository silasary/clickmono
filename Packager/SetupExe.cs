using System;
using System.Runtime.InteropServices;


namespace Bootstrapper
{
    static class Program
    {
        public static readonly bool IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);

        [DllImport("dfshim", CharSet = CharSet.Unicode)]
        static extern int LaunchApplication(string deploymentUrl, IntPtr data, int flags);

        public static readonly string DeploymentUrl = "__URL__";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && !IsRunningOnMono)
            {
                LaunchApplication(DeploymentUrl, IntPtr.Zero, 0);
            }
            else
            {
                // TODO: Implement me
                Console.WriteLine("Bootstrapper doesn't currently work on Mono.  Sorry!");
            }

        }
    }
}
