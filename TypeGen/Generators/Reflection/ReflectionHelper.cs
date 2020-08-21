using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                    throw new ApplicationException($"Error when acquiring interfaces map from {type}, interface:{intf}", ex);
                }
            }
            return result;
        }

        public static string MethodToString(MethodInfo m, bool useFullTypeName = true)
        {
            var sb = new StringBuilder();
            //if (m.ReturnType == typeof(Task) || m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition()==typeof(Task<>))
            //{
            //    sb.Append("async Task");
            //    if (m.ReturnType.IsGenericType)
            //    {
            //        sb.Append("<");
            //        sb.Append(TypeToString(m.ReturnType.GetGenericArguments()[0]));
            //        sb.Append(">");
            //    }
            //}
            //else
            //{
                sb.Append(TypeToString(m.ReturnType, useFullTypeName));
            //}
            sb.Append(" ");
            sb.Append(m.Name);
            sb.Append("(");
            foreach (var p in m.GetParameters())
            {
                if (p != m.GetParameters().First())
                    sb.Append(", ");
                sb.Append(TypeToString(p.ParameterType, useFullTypeName));
                sb.Append(" ");
                sb.Append(p.Name);
                if (p.HasDefaultValue)
                {
                    sb.Append(" = ");
                    sb.Append(p.DefaultValue ?? "null");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string TypeToString(Type type, bool useFullTypeName = true)
        {
            if (!type.IsGenericType)
                return ShortenTypeName(type, useFullTypeName);
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return ShortenTypeName(type.GetGenericArguments()[0], useFullTypeName) + "?";
            }
            var sb = new StringBuilder();
            sb.Append(ShortenTypeName(type.GetGenericTypeDefinition(), useFullTypeName));
            sb.Append("<");
            foreach (var ga in type.GetGenericArguments())
            {
                sb.Append(TypeToString(ga, useFullTypeName));
            }
            sb.Append(">");
            return sb.ToString();
        }

        private static string ShortenTypeName(Type type, bool useFullTypeName = true)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int))
                return "int";
            if (type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal))
            {
                return Generators.NamingHelper.GetNonGenericTypeName(type);
            }
            return (useFullTypeName ? (type.Namespace + ".") : "") + Generators.NamingHelper.GetNonGenericTypeName(type);
        }
    }
}
