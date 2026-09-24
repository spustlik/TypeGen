using System;
using System.ComponentModel;
using System.Reflection;

namespace TypeGen.Generators.WebApi
{

    public interface IWebApiReflection
    {
        string GetActionNameAttribute(MethodInfo m);
        bool GetFromUriAttribute(ParameterInfo p, out string name);
        string GetHttpMethodAttribute(MethodInfo m);
        string GetRoutePrefixAttribute(MemberInfo m);
        string GetRouteTemplateAttribute(MemberInfo m);
        string GetTypeGenDescription(Type t);
        string GetTypeGenDescription(MethodInfo m);
        string GetTypeGenDescription(DescriptionAttribute at);
        bool HasHttpBodyParamAttribute(ParameterInfo pi);
        bool HasNonActionAttribute(MethodInfo m);
        bool IsHttpResponseMessage(Type type);

    }
}
