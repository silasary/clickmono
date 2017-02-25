using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Metadata;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
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
        //public static int GenerateBootstrap(string DeploymentURL, string exepath)
        //{
        //    Console.WriteLine($"Generating Boostrapper {exepath}");
        //    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Bootstrapper.exe");
        //    var setup = WindowsAssembly.FromFile(path);
        //    var tableStream = (TableStream)setup.NetDirectory.MetadataHeader.GetStream("#~");
        //    var types = tableStream.GetTable<TypeDefinition>();
        //    var Program = types.First(t => t.FullName == "Bootstrapper.Program");
        //    var cctor = Program.Methods.Single(m => m.Name == ".cctor");
        //    var body = cctor.MethodBody;
        //    var ldstr = body.Instructions.Where(i => i.OpCode == AsmResolver.Net.Msil.MsilOpCodes.Ldstr && i.Operand.Equals("http://katelyngigante.com/deployment/clickmono/ClickMac.application")).Single();
        //    ldstr.Operand = DeploymentURL;
        //    setup.Write(exepath);
        //    return 0;
        //}

        public static int GenerateBootstrap(string DeploymenyURL, string exepath)
        {
            string code;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Packager.SetupExe.cs"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    code = reader.ReadToEnd();
                }

            }
            var snip = code.Replace("__URL__", DeploymenyURL);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.GenerateExecutable = true;
            cp.OutputAssembly = exepath;
            cp.CompilerOptions = "/t:winexe";
            cp.GenerateInMemory = false;
            CompilerResults cr = provider.CompileAssemblyFromSource(cp, snip);

            if (cr.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building {0} into {1}",
                    "Bootstrapper", cr.PathToAssembly);
                foreach (CompilerError ce in cr.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("Bootstrapper for {0} built into {1} successfully.",
                    DeploymenyURL, cr.PathToAssembly);
            }

            // Return the results of compilation.
            if (cr.Errors.Count > 0)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }
    }
}
