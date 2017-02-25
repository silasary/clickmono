using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Packager
{
    class Bootstrapper
    {
        public static int GenerateBootstrap(string DeploymentURL, string exepath)
        {
            Console.WriteLine($"Generating Boostrapper {exepath}");
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Bootstrapper.exe");
            var setup = WindowsAssembly.FromFile(path);
            var tableStream = (TableStream)setup.NetDirectory.MetadataHeader.GetStream("#~");
            var types = tableStream.GetTable<TypeDefinition>();
            var Program = types.First(t => t.FullName == "Bootstrapper.Program");
            var cctor = Program.Methods.Single(m => m.Name == ".cctor");
            var body = cctor.MethodBody;
            var ldstr = body.Instructions.Where(i => i.OpCode == AsmResolver.Net.Msil.MsilOpCodes.Ldstr && i.Operand.Equals("http://katelyngigante.com/deployment/clickmono/ClickMac.application")).Single();
            ldstr.Operand = DeploymentURL;
            setup.Write(exepath);
            return 0;
        }
    }
}
