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

        public static bool IsTypeBaseOrSelf(this Type type, string fullTypeName)
        {
            if (type.FullName == fullTypeName)
                return true;
            //inheritance
            if (type.BaseType != type && type.BaseType != null && type.BaseType != typeof(object))
                return type.BaseType.IsTypeBaseOrSelf(fullTypeName);
            return false;
        }

        public static bool IsTypeImplementingInterface(this Type type, string interfaceFullName)
        {
            return type.GetInterfaces().Any(i => i.IsTypeBaseOrSelf(interfaceFullName));
        }

        public static IEnumerable<Type> GetImplementedInterfaces(this Type type)
        {
            var allInterfaces = type.GetInterfaces();
            if (type.IsInterface)
                return allInterfaces;
            var result = new List<Type>();
            //return allInterfaces.Where(intf => type.GetInterfaceMap(intf).TargetMethods.Any(m => m.DeclaringType == type));
            foreach (var intf in allInterfaces)
            {
                try
                {
                    var map = type.GetInterfaceMap(intf);
                    if (map.TargetMethods.Any(m => m.DeclaringType == type))
                    {
                        result.Add(intf);
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Error when acquiring interfaces map from " + type + ", interface:" + intf, ex);
                }
            }
            return result;
        }


    }
}
