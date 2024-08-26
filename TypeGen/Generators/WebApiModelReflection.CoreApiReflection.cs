using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TypeGen.Generators.WebApi
{
    public class CoreApiReflection : NetApiReflection, IWebApiReflection
    {
        public CoreApiReflection()
        {
            HTTPMETHOD_AT = "Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider";
            HTTPRESPONSETYPE = "System.Net.Http.HttpResponseMessage";            
            NONACTIONTYPE = "Microsoft.AspNetCore.Mvc.NonActionAttribute";
            ACTIONNAME_AT = "Microsoft.AspNetCore.Mvc.ActionNameAttribute";
            FROMBODY_AT = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";
            ROUTE_AT = "Microsoft.AspNetCore.Mvc.RouteAttribute";
            FROMURI_AT = "Microsoft.AspNetCore.Mvc.FromQueryAttribute";
            
            //non-exists ! ROUTEPREFIX_AT = "Microsoft.AspNetCore.Mvc.RoutePrefixAttribute";
        }

        public override string GetHttpMethodAttribute(MethodInfo m)
        {
            var httpMethodAt = m.GetCustomAttributes(true)
                .FirstOrDefault(at => at.GetType().IsTypeImplementingInterface(HTTPMETHOD_AT));
            if (httpMethodAt != null)
            {
                var at = (dynamic)httpMethodAt;
                if (at.HttpMethods.Count > 0)
                {
                    return at.HttpMethods[0];
                }
            }
            return null;
        }

    }
}
