using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeGen;
using TypeGen.Generators;

namespace TestWebApi
{
    class Program
    {
        static void Main(string[] args)
        {
            var model = typeof(CloudSupportApi.Model.MonitorModel);
            

            var r = new ReflectionGenerator();
            r.NamingStrategy.InterfacePrefix = "";
            r.NamingStrategy.InterfacePrefixForClasses = "";
            r.GenerationStrategy.CommentSource = true;
            r.GenerateTypes(ReflectionHelper.NamespaceTypes(model));

            r.Module.Name = "Models";

            var g = new OutputGenerator();
            DefaultNameResolver.ThisModule = r.Module;

            manager.StartNewFile("api.models.ts");

            var controllerTypes = model.Assembly.GetTypes().Where(t => t.BaseType == typeof(System.Web.Http.ApiController)).ToArray();
            var apigen = new WebApiControllerGenerator();
            var controllers = apigen.GetControllersModel(controllerTypes);

            var apiModule = new TypescriptModule("Api");
            //this will generate new proxy classes into apiModule and models into r.Module
            apigen.GenerateControllers(controllers, r, apiModule);

            r.Module.MakeAllExportable(true);

            g.GenerateAmbient(r.Module);
            g.Formatter.WriteLine();
            g.GenerateNonAmbient(r.Module);
            WriteLine(g.Output);

            g.Formatter.Output.Clear();


            //manager.StartNewFile("api.observables.ts");
            var oModule = new TypescriptModule("Observables");
            new KnockoutGenerator().GenerateObservableModule(r.Module, oModule, interfaces: false);
            oModule.MakeAllExportable(true);
            DefaultNameResolver.AddModule(r.Module, "Models");
            g.GenerateModuleContent(oModule, null);
            WriteLine("import ko = require('knockout');");
            WriteLine(g.Output);
            g.Formatter.Output.Clear();

            manager.Process(true);

            apiModule.Members.Insert(0, new RawStatements("import base = require('backend/proxyBase');"));

            DefaultNameResolver.ThisModule = apiModule;
            DefaultNameResolver.AddModule(r.Module);
            g.GenerateModuleContent(apiModule, null);
            WriteLine(g.Output);
            g.Formatter.Output.Clear();


            Console.ReadLine();
        }

        static void WriteLine(string s = null)
        {
            Console.WriteLine(s);
        }

        private static ManagerEmu manager = new ManagerEmu();
        class ManagerEmu
        {
            internal void Process(bool v)
            {
                
            }

            internal void StartNewFile(string v)
            {
                Console.WriteLine("-----" + v + "-----");
            }
        }
    }
}
