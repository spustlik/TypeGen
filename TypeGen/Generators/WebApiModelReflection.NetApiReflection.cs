using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TypeGen.Generators.WebApi
{
    public class NetApiReflection : IWebApiReflection
    {
        protected string HTTPRESPONSETYPE = "System.Net.Http.HttpResponseMessage";
        protected string NONACTIONTYPE = "System.Web.Http.NonActionAttribute";
        protected string FROMURI_AT = "System.Web.Http.FromUriAttribute";
        protected string ACTIONNAME_AT = "System.Web.Http.ActionNameAttribute";
        protected string FROMBODY_AT = "System.Web.Http.FromBodyAttribute";
        protected string ROUTEPREFIX_AT = "System.Web.Http.RoutePrefixAttribute";
        protected string HTTPMETHOD_AT = "System.Web.Http.Controllers.IActionHttpMethodProvider";
        protected string ROUTE_AT = "System.Web.Http.RouteAttribute";

        public bool IsHttpResponseMessage(Type type)
        {
            return type.IsTypeBaseOrSelf(HTTPRESPONSETYPE);
        }

        public bool HasNonActionAttribute(MethodInfo m)
        {
            var nonActionAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf(NONACTIONTYPE));
            return nonActionAt != null;
        }

        public bool GetFromUriAttribute(ParameterInfo p, out string name)
        {
            var fromUriAt = p.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf(FROMURI_AT));
            if (fromUriAt != null)
            {
                name = ((dynamic)fromUriAt).Name;
                return true;
            }
            name = null;
            return false;
        }
        public string GetActionNameAttribute(MethodInfo m)
        {
            var actionNameAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf(ACTIONNAME_AT));
            if (actionNameAt != null)
            {
                return ((dynamic)actionNameAt).Name;
            }
            return null;
        }
        public bool HasHttpBodyParamAttribute(ParameterInfo pi)
        {
            return pi.GetCustomAttributes(false).Any(at => at.GetType().IsTypeBaseOrSelf(FROMBODY_AT));
        }
        public string GetRoutePrefixAttribute(MemberInfo m, string defaultValue)
        {
            var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf(ROUTEPREFIX_AT));
            if (routeAt != null)
            {
                return ((dynamic)routeAt).Prefix;
            }
            return defaultValue;
        }
        public string GetRouteTemplateAttribute(MethodInfo m)
        {
            var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf(ROUTE_AT));
            if (routeAt != null)
            {
                return ((dynamic)routeAt).Template;
            }
            return null;
        }

        public string GetTypeGenDescription(Type t)
        {
            return GetTypeGenDescription(t.GetCustomAttribute<DescriptionAttribute>());
        }
        public string GetTypeGenDescription(MethodInfo m)
        {
            return GetTypeGenDescription(m.GetCustomAttribute<DescriptionAttribute>());
        }

        public string GetTypeGenDescription(DescriptionAttribute at)
        {
            if (at == null)
                return null;
            var s = at.Description?.Trim();
            const string PREFIX = "TYPEGEN:";
            if (!s.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
                return null;
            return s.Substring(PREFIX.Length);
        }

        public virtual string GetHttpMethodAttribute(MethodInfo m)
        {
            var httpMethodAt = m.GetCustomAttributes(true)
                .FirstOrDefault(at => at.GetType().IsTypeImplementingInterface(HTTPMETHOD_AT));
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
