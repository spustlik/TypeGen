using System;
using System.Reflection;

namespace TypeGen
{
    public interface INamingStrategy
    {
        string GetClassName(Type type);
        string GetEnumMemberName(FieldInfo value);
        string GetEnumName(Type type);
        string GetGenericArgumentName(Type garg);
        string GetInterfaceName(Type type);
        string GetMethodName(MethodInfo method);
        string GetPropertyName(PropertyInfo property);
    }
}