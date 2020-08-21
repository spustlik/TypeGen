using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TypeGen.Generators.WebApi
{
    public partial class WebApiModelReflection
    {
        public static class Reflection
        {
            public static bool IsHttpResponseMessage(Type type)
            {
                return type.IsTypeBaseOrSelf("System.Net.Http.HttpResponseMessage");
            }

            public static bool HasNonActionAttribute(MethodInfo m)
            {
                var nonActionAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.NonActionAttribute"));
                return nonActionAt != null;
            }

            public static bool GetFromUriAttribute(ParameterInfo p, out string name)
            {
                var fromUriAt = p.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.FromUriAttribute"));
                if (fromUriAt != null)
                {
                    name = ((dynamic)fromUriAt).Name;
                    return true;
                }
                name = null;
                return false;
            }
            public static string GetActionNameAttribute(MethodInfo m)
            {
                var actionNameAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.ActionNameAttribute"));
                if (actionNameAt != null)
                {
                    return ((dynamic)actionNameAt).Name;
                }
                return null;
            }
            public static bool HasHttpBodyParamAttribute(ParameterInfo pi)
            {
                return pi.GetCustomAttributes(false).Any(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.FromBodyAttribute"));
            }
            public static string GetRoutePrefixAttribute(MemberInfo m, string defaultValue)
            {
                var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.RoutePrefixAttribute"));
                if (routeAt != null)
                {
                    return ((dynamic)routeAt).Prefix;
                }
                return defaultValue;
            }
            public static string GetRouteTemplateAttribute(MethodInfo m)
            {
                var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.RouteAttribute"));
                if (routeAt != null)
                {
                    return ((dynamic)routeAt).Template;
                }
                return null;
            }

            public static string GetTypeGenDescription(Type t)
            {
                return GetTypeGenDescription(t.GetCustomAttribute<DescriptionAttribute>());
            }
            public static string GetTypeGenDescription(MethodInfo m)
            {
                return GetTypeGenDescription(m.GetCustomAttribute<DescriptionAttribute>());
            }

            public static string GetTypeGenDescription(DescriptionAttribute at)
            {
                if (at == null)
                    return null;
                var s = at.Description?.Trim();
                const string PREFIX = "TYPEGEN:";
                if (!s.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
                    return null;
                return s.Substring(PREFIX.Length);
            }

            public static string GetHttpMethodAttribute(MethodInfo m)
            {
                var httpMethodAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeImplementingInterface("System.Web.Http.Controllers.IActionHttpMethodProvider"));
                if (httpMethodAt != null)
                {
                    var at = (dynamic)httpMethodAt;
                    if (at.HttpMethods.Count > 0)
                    {
                        return at.HttpMethods[0].Method;
                    }
                }
                return null;
            }

        }
    }


}
