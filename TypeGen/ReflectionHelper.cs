using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public static class ReflectionHelper
    {
        public static IEnumerable<Type> NamespaceTypes(Type type)
        {
            return type.Assembly.GetTypes().Where(t => t.Namespace == type.Namespace).OrderBy(t=>t.Name);
        }

        //TODO: remove
        public static OutputGenerator GenerateReflection(this OutputGenerator g, ReflectionGenerator rg)
        {
            foreach (var item in rg.Enums.Reverse())
            {
                //TODO: accessibility, modules
                g.Generate(item);
                g.Formatter.WriteLine();
            }
            foreach (var item in rg.Declarations.Reverse())
            {
                //TODO: accessibility, modules
                g.Generate(item);
                g.Formatter.WriteLine();
            }

            return g;
        }

    }
}
