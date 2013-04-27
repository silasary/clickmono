using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kamahl.Deployment
{
   public sealed class HostingManager
    {
       private readonly Uri DeploymentUri;
       private readonly bool LaunchInHostProcess;
       private bool EchoToConsole;
       public HostingManager(Uri DeploymentUri, bool LaunchInHostProcess)
       {
           this.DeploymentUri = DeploymentUri;
           this.LaunchInHostProcess = LaunchInHostProcess;
       }

       public void Download()
       {
           //new Loading(this).ReadManifest(DeploymentUri.ToString());

       }

       internal void Info(string format, params string[] args)
       {
           if (this.EchoToConsole)
               Console.WriteLine(format, args);
       }
       
    }
}
