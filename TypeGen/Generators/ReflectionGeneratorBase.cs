using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public class ReflectionGeneratorBase
    {
        private Dictionary<Type, TypescriptTypeBase> _typeMap = new Dictionary<Type, TypescriptTypeBase>();

        public const string SOURCETYPE_KEY = "SOURCE_TYPE";
        public const string SOURCEMEMBER_KEY = "SOURCE_PROPERTY";

        public INamingStrategy NamingStrategy { get; private set; }
        public IGenerationStrategy GenerationStrategy { get; private set; }

        public ReflectionGeneratorBase(INamingStrategy naming, IGenerationStrategy generation)
        {
            _typeMap = new Dictionary<Type, TypescriptTypeBase>()
            {
                { typeof(void), PrimitiveType.Void },
                { typeof(object), PrimitiveType.Any },
                { typeof(int), PrimitiveType.Number },
                { typeof(double), PrimitiveType.Number },
                { typeof(float), PrimitiveType.Number },
                { typeof(string), PrimitiveType.String },
                { typeof(bool), PrimitiveType.Boolean },
                { typeof(DateTime), PrimitiveType.Date },
            };
            NamingStrategy = naming;
            GenerationStrategy = generation;
        }

        public TypescriptTypeReference GenerateFromType(Type type)
        {
            TypescriptTypeBase result;
            if (_typeMap.TryGetValue(type, out result))
                return result;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                var r = GenerateFromType(Nullable.GetUnderlyingType(type));
                r.ExtraData[SOURCETYPE_KEY] = type;
                return r;
            }
            if (type.IsPrimitive)
                return PrimitiveType.Number; //others are in dictionary            
            if (type.IsGenericParameter)
            {
                //TODO:?!?
                //return new GenericParameter() { Name = NamingStrategy.GetGenericArgumentName(type), ExtraData = { { SOURCETYPE_KEY, type } } };
                return new TypescriptTypeReference(NamingStrategy.GetGenericArgumentName(type)) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (type.IsArray)
            {
                if (type.GetArrayRank() == 1)
                    return new ArrayType(GenerateFromType(type.GetElementType())) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsConstructedGenericType)
                    return new ArrayType(GenerateFromType(type.GetGenericArguments()[0])) { ExtraData = { { SOURCETYPE_KEY, type } } };
            }
            if (type.IsEnum)
            {
                return GenerateEnum(type);
            }
            if (type.IsClass || type.IsInterface)
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    var tref = GenerateFromType(type.GetGenericTypeDefinition());
                    foreach (var genericTypeArgument in type.GenericTypeArguments)
                    {
                        if (GenerationStrategy.ShouldGenerateGenericTypeArgument(result, genericTypeArgument))
                        {
                            tref.GenericParameters.Add(GenerateFromType(genericTypeArgument));
                        }
                    }
                    return tref;
                }
                return GenerateClassDeclaration(type);
            }

            return new AnyType() { ExtraData = { { SOURCETYPE_KEY, type } } };
        }


        private DeclarationBase GenerateClassDeclaration(Type type)
        {
            if (GenerationStrategy.ShouldGenerateClass(type))
            {
                return GenerateClass(type);
            }
            else
            {
                return GenerateInterface(type);
            }
        } 
        private void GenerateMethodDeclarations(Type type, DeclarationBase declaration)
        {
            //method declarations
            if (GenerationStrategy.ShouldGenerateMethods(declaration, type))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (!GenerationStrategy.ShouldGenerateMethod(declaration, method))
                        continue;
                    declaration.Members.Add(GenerateMethodDeclaration(method));
                }
            }
        }

        public FunctionDeclarationMember GenerateMethodDeclaration(MethodInfo method)
        {
            var result = new FunctionDeclarationMember(NamingStrategy.GetMethodName(method)) { ExtraData = { { SOURCEMEMBER_KEY, method } } };
            if (method.ReturnType != typeof(void))
            {
                result.ResultType = GenerateFromType(method.ReturnType);
            }
            else
            {
                result.ResultType = PrimitiveType.Void;
            }
            foreach (var p in method.GetParameters())
            {
                var par = GenerateMethodParameter(p);
                result.Parameters.Add(par);
            }
            //generics
            if (method.IsGenericMethod)
            {
                foreach (var garg in method.GetGenericArguments())
                {
                    result.GenericParameters.Add(new GenericParameter(NamingStrategy.GetGenericArgumentName(garg)) { ExtraData = { { SOURCETYPE_KEY, garg } } });
                }
            }

            return result;
        }

        private FunctionParameter GenerateMethodParameter(ParameterInfo paramInfo)
        {
            var par = new FunctionParameter(paramInfo.Name)
            {
                ParameterType = GenerateFromType(paramInfo.ParameterType),
                ExtraData = { { SOURCEMEMBER_KEY, paramInfo } },
                IsOptional = paramInfo.IsOptional
            };
            //if (ParameterType)
            if (paramInfo.DefaultValue != null)
            {
                par.DefaultValue = GenerationStrategy.GenerateLiteral(paramInfo.DefaultValue, par.ParameterType);
            }
            if (paramInfo.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                par.IsRest = true;
            }
            return par;
        }

        public InterfaceType GenerateInterface(Type type)
        {
            var result = new InterfaceType(NamingStrategy.GetInterfaceName(type));
            GenerateDeclarationBase(result, type);
            //extends from .Base 
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                if (GenerationStrategy.ShouldGenerateBaseClass(result, type, type.BaseType))
                {
                    result.ExtendsTypes.Add(GenerateFromType(type.BaseType));
                }
            }
            //implemented interfaces as extends
            if (GenerationStrategy.ShouldGenerateImplementedInterfaces(result, type))
            {
                var implemented = GetImplementedInterfaces(type).ToArray();
                foreach (var intf in implemented)
                {
                    if (GenerationStrategy.ShouldGenerateImplementedInterface(result, intf))
                    {
                        result.ExtendsTypes.Add(GenerateFromType(intf));
                    }
                }
            }
            GenerateMethodDeclarations(type, result);
            return result;
        }

        private static IEnumerable<Type> GetImplementedInterfaces(Type type)
        {
            var allInterfaces = type.GetInterfaces();
            if (type.IsInterface)
                return allInterfaces;
            var result = new List<Type>();
            //return allInterfaces.Where(intf => type.GetInterfaceMap(intf).TargetMethods.Any(m => m.DeclaringType == type));
            foreach (var intf in allInterfaces)
            {
                try {
                    var map = type.GetInterfaceMap(intf);
                    if (map.TargetMethods.Any(m => m.DeclaringType == type))
                    {
                        result.Add(intf);
                    }
                }
                catch(Exception ex)
                {
                    throw new ApplicationException("Error when acquiring interfaces map from " + type + ", interface:" + intf, ex);
                }
            }
            return result;            
        }

        public ClassType GenerateClass(Type type)
        {
            var result = new ClassType(NamingStrategy.GetClassName(type));
            GenerateDeclarationBase(result, type);
            //extends from .Base 
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                if (GenerationStrategy.ShouldGenerateBaseClass(result, type, type.BaseType))
                {
                    result.Extends = GenerateFromType(type.BaseType);
                }
            }
            //implemented interfaces as implements
            if (GenerationStrategy.ShouldGenerateImplementedInterfaces(result, type))
            {
                var implemented = GetImplementedInterfaces(type);
                foreach (var intf in implemented)
                {
                    if (GenerationStrategy.ShouldGenerateImplementedInterface(result, intf))
                    {
                        result.Implementations.Add(GenerateFromType(intf));
                    }
                }
            }
            GenerateMethodDeclarations(type, result);
            return result;
        }

        private void GenerateDeclarationBase(DeclarationBase result, Type type)
        {
            result.ExtraData[SOURCETYPE_KEY] = type;
            _typeMap[type] = result;
            GenerationStrategy.AddDeclaration(result);            

            //generics
            if (type.IsGenericType)
            {
                foreach (var garg in type.GetGenericArguments())
                {
                    result.GenericParameters.Add(new GenericParameter(NamingStrategy.GetGenericArgumentName(garg)) { ExtraData = { { SOURCETYPE_KEY, garg} } });
                }                
            }

            //properties
            if (GenerationStrategy.ShouldGenerateProperties(result, type))
            {
                var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);
                foreach (var pi in allProps)
                {
                    if (!GenerationStrategy.ShouldGenerateProperty(result, pi))
                        continue;
                    var p = GenerateProperty(pi);
                    result.Members.Add(p);
                }
            }            
        }

        public DeclarationMember GenerateProperty(PropertyInfo pi)
        {
            var pm = new PropertyMember(NamingStrategy.GetPropertyName(pi))
            {
                ExtraData = { { SOURCEMEMBER_KEY, pi } },
                IsOptional = Nullable.GetUnderlyingType(pi.PropertyType) != null,
                MemberType = GenerateFromType(pi.PropertyType)
            };
            return pm;
        }

        public TypescriptTypeReference GenerateEnum(Type type)
        {
            var enumType = new EnumType(NamingStrategy.GetEnumName(type)) { ExtraData = { { SOURCETYPE_KEY, type } } };
            _typeMap[type] = enumType;
            GenerationStrategy.AddDeclaration(enumType);
            var values = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var value in values)
            {
                enumType.Members.Add(GenerateEnumMember(value));
            }
            return enumType;
        }

        public EnumMember GenerateEnumMember(FieldInfo value)
        {
            var v = (int)value.GetValue(null);
            var ev = new EnumMember(NamingStrategy.GetEnumMemberName(value), v) { ExtraData = { { SOURCEMEMBER_KEY, value } } };
            return ev;
        }


    }

}
