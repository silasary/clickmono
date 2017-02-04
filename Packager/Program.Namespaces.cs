using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Packager
{
    partial class Program
    {
        static XNamespace asmv1ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
        static XNamespace asmv2ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v2");
        static XNamespace asmv3ns = XNamespace.Get("urn:schemas-microsoft-com:asm.v3");
        static XNamespace clickoncev1ns = XNamespace.Get("urn:schemas-microsoft-com:clickonce.v1");
        static XNamespace clickoncev2ns = XNamespace.Get("urn:schemas-microsoft-com:clickonce.v2");
        static XNamespace dsigns = XNamespace.Get("http://www.w3.org/2000/09/xmldsig#");


        static XName asmv1assembly = asmv1ns.GetName("assembly");
        static XName asmv1assemblyIdentity = asmv1ns.GetName("assemblyIdentity");
        static XName asmv1name = asmv1ns.GetName("name");
        static XName asmv1version = asmv1ns.GetName("version");
        static XName asmv1publicKeyToken = asmv1ns.GetName("publicKeyToken");
        static XName asmv1language = asmv1ns.GetName("language");
        static XName asmv1processorArchitecture = asmv1ns.GetName("processorArchitecture");
        static XName asmv1description = asmv1ns.GetName("description");
        static XName asmv2publisher = asmv2ns.GetName("publisher");
        static XName asmv2product = asmv2ns.GetName("product");
        static XName asmv2deployment = asmv2ns.GetName("deployment");
        static XName asmv2install = asmv2ns.GetName("install");
        static XName asmv2mapFileExtensions = asmv2ns.GetName("mapFileExtensions");
        static XName asmv2subscription = asmv2ns.GetName("subscription");
        static XName asmv2update = asmv2ns.GetName("update");
        static XName asmv2beforeApplicationStartup = asmv2ns.GetName("beforeApplicationStartup");
        static XName asmv2deploymentProvider = asmv2ns.GetName("deploymentProvider");
        static XName asmv2codebase = asmv2ns.GetName("codebase");
        static XName asmv2dependency = asmv2ns.GetName("dependency");
        static XName asmv2dependentAssembly = asmv2ns.GetName("dependentAssembly");
        static XName asmv2dependencyType = asmv2ns.GetName("dependencyType");
        static XName asmv2size = asmv2ns.GetName("size");
        static XName asmv2assemblyIdentity = asmv2ns.GetName("assemblyIdentity");
        static XName asmv2name = asmv2ns.GetName("name");
        static XName asmv2version = asmv2ns.GetName("version");
        static XName asmv2publicKeyToken = asmv2ns.GetName("publicKeyToken");
        static XName asmv2language = asmv2ns.GetName("language");
        static XName asmv2processorArchitecture = asmv2ns.GetName("processorArchitecture");
        static XName asmv2type = asmv2ns.GetName("type");
        static XName asmv2hash = asmv2ns.GetName("hash");
        static XName clickoncev2compatibleFrameworks = clickoncev2ns.GetName("compatibleFrameworks");
        static XName clickoncev2framework = clickoncev2ns.GetName("framework");
        static XName clickoncev2targetVersion = clickoncev2ns.GetName("targetVersion");
        static XName clickoncev2profile = clickoncev2ns.GetName("profile");
        static XName clickoncev2supportedRuntime = clickoncev2ns.GetName("supportedRuntime");
        static XName dsigTransforms = dsigns.GetName("Transforms");
        static XName dsigTransform = dsigns.GetName("Transform");
        static XName dsigAlgorithm = XName.Get("Algorithm");// dsigns.GetName("Algorithm");
        static XName dsigDigestMethod = dsigns.GetName("DigestMethod");
        static XName dsigDigestValue = dsigns.GetName("DigestValue");


        static XName asmv2application = asmv2ns.GetName("application");
        static XName asmv2entryPoint = asmv2ns.GetName("entryPoint");
        static XName asmv2trustInfo = asmv2ns.GetName("trustInfo");
        static XName asmv2security = asmv2ns.GetName("security");
        static XName asmv2applicationRequestMinimum = asmv2ns.GetName("applicationRequestMinimum");
        static XName asmv2PermissionSet = asmv2ns.GetName("PermissionSet");
        static XName asmv2defaultAssemblyRequest = asmv2ns.GetName("defaultAssemblyRequest");
        static XName asmv2file = asmv2ns.GetName("file");
        static XName asmv2commandLine = asmv2ns.GetName("commandLine");
        static XName asmv2dependentOS = asmv2ns.GetName("dependentOS");
        static XName asmv2osVersionInfo = asmv2ns.GetName("osVersionInfo");
        static XName asmv2os = asmv2ns.GetName("os");
        static XName asmv3requestedPrivileges = asmv3ns.GetName("requestedPrivileges");
        static XName asmv3requestedExecutionLevel = asmv3ns.GetName("requestedExecutionLevel");
        static XName clickoncev1useManifestForTrust = clickoncev1ns.GetName("useManifestForTrust");

    }
}
