using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators.WebApi
{
    public class WebApiProxyGenerator2
    {
        protected readonly ReflectionGeneratorBase _reflectionGenerator;
        public string PromiseTypeName = "Promise";
        public string GeneratedClassName = "GeneratedProxy";
        public string ProxyBaseName = "base.ProxyBase";

        public WebApiProxyGenerator2(ReflectionGeneratorBase reflectionGenerator)
        {
            _reflectionGenerator = reflectionGenerator;
        }
        public void GenerateControllers(IEnumerable<ControllerModel> controllers, TypescriptModule targetModule)
        {
            var proxyClass = new ClassType("GeneratedProxy");
            if (!String.IsNullOrEmpty(ProxyBaseName))
            {
                proxyClass.Extends = new TypescriptTypeReference(ProxyBaseName);
            }
            targetModule.Members.Add(new DeclarationModuleElement(proxyClass) { IsExporting = true });
            foreach (var controller in controllers)
            {
                GenerateController(targetModule, proxyClass, controller);
            }
            //targetModule.Members.Add(new RawStatements("export var proxy = new ", proxyClass, "();"));
        }

        protected virtual void GenerateController(TypescriptModule targetModule, ClassType proxyClass, ControllerModel controller)
        {
            var actions = controller.Actions.Select(x => GenerateAction(x)).ToArray();
            var obj = new AnonymousDeclaration();
            foreach (var item in actions)
            {
                obj.Members.Add(item);
            }
            proxyClass.Members.Add(new PropertyMember(controller.Name.Replace("Controller", ""))
            {
                Accessibility = AccessibilityEnum.Public,
                Comment = controller.Comment,
                Initialization = new RawStatements(obj)
            });
        }

        protected virtual FunctionMember GenerateAction(ActionModel action)
        {
            var fn = new FunctionMember(action.Name + "Async", null)
            {
                Accessibility = AccessibilityEnum.Public,
                Comment = GenerateActionComment(action),
                //arrow function is used to simplify usage of "this"
                Style = FunctionStyle.ArrowFunction
            };
            GenerateMethodParametersSignature(action, fn);
            if (action.ResultType != null)
            {
                fn.ResultType = new TypescriptTypeReference(PromiseTypeName) { GenericParameters = { _reflectionGenerator.GenerateFromType(action.ResultType) } };
            }
            else
            {
                fn.ResultType = new TypescriptTypeReference(PromiseTypeName) { GenericParameters = { PrimitiveType.Void } };
            }

            fn.Body = new RawStatements();
            fn.Body.Statements.Add("this.call" + action.HttpMethod + "(");
            GenerateUrlParametersValue(action, fn);
            fn.Body.Statements.Add(", ");
            GenerateNamedParametersValue(action, fn);
            var dataParam = action.Parameters.FirstOrDefault(p => p.IsData);
            if (dataParam != null)
            {
                fn.Body.Statements.Add(", " + dataParam.Name);
                if (dataParam.IsData)
                {
                    fn.Body.Statements.Add("/* DATA */");
                }
            }
            fn.Body.Statements.Add(")");
            //return FunctionToDelegate(method);
            return fn;
        }

        private RawStatements FunctionToDelegate(FunctionMember method)
        {
            var result = new RawStatements();
            if (!String.IsNullOrEmpty(method.Comment))
            {
                result.Add("/*");
                result.Add(method.Comment);
                result.Add("*/\n");
            }
            result.Add(method.Name);
            result.Add(": (");
            bool isFirst = true;
            foreach (var item in method.Parameters)
            {
                if (!isFirst)
                    result.Add(", ");
                isFirst = false;
                result.Add(item.Name);
                if (item.ParameterType != null)
                {
                    result.Add(": ");
                    result.Add(item.ParameterType);
                }
                //TODO:if (item.IsOptional)
            }
            result.Add(")");
            if (method.ResultType != null)
            {
                result.Add(": ");
                result.Add(method.ResultType);
            }
            result.Add(" => ");
            result.Add(method.Body);
            return result;
        }

        protected virtual string GenerateActionComment(ActionModel action)
        {
            var sb = new StringBuilder();
            sb.Append("*");
            sb.Append(action.Comment);
            if (!sb.ToString().EndsWith("\n"))
                sb.Append("\n");
            sb.Append(" parameters: ").Append(String.Join(", ", action.Parameters));
            return sb.ToString();
        }

        protected virtual void GenerateUrlParametersValue(ActionModel action, FunctionMember method)
        {
            var urlVar = $"'{action.UrlTemplate.TrimEnd('/')}'";
            foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
            {
                var paramName = p.UrlName ?? p.Name;
                urlVar = urlVar.Replace("{" + paramName + "}", $"' + {p.Name} + '");
            }
            const string EMPTYJS = " + ''";
            while (urlVar.EndsWith(EMPTYJS, StringComparison.InvariantCulture))
            {
                urlVar = urlVar.Substring(0, urlVar.Length - EMPTYJS.Length);
            }

            method.Body.Statements.Add(urlVar);
        }

        protected virtual void GenerateMethodParametersSignature(ActionModel action, FunctionMember method)
        {
            method.Parameters.AddRange(action.Parameters
                .Where(p => !p.IsOptional)
                .Select(p => new FunctionParameter(p.Name)
                {
                    ParameterType = _reflectionGenerator.GenerateFromType(p.Type),
                    IsOptional = false
                }));
            //consider: 
            //  if there is only one optional parameter, or all opt. parameters are last (it must be in c# decl), 
            //  we can generate myMethod(p1,p2,..., po1:string? = null, po2:number? = null)
            //  but, it is needed then to call it with positional params (TypeScript doesn't know named params)
            //  xxx.myMethod("asd","qwe",...,null, 42)
            //compare to: call via optional properties of anonymous object
            //  xxx.myMethod("asd","qwe",..., { po2: 42} )
            // BUT, it can be called with any object, because all positional parameters are optional, so any object will match
            //  xxx.myMethod("asd","qwe",..., "nonsense" )
            // partially solved:
            //  positional parameters are unions of non-nullable parameter objects (see code)
            //  but this approach will still validate only one of parameters, others are than ignored (because at least one matched)
            if (action.Parameters.Any(p => p.IsOptional))
            {
                var optParams = action.Parameters
                    .Where(p => p.IsOptional)
                    .Select(p => new RawStatements("{ ", p.Name, ": ", _reflectionGenerator.GenerateFromType(p.Type), " }"))
                    .ToArray();
                var raw = new RawStatements();
                foreach (var item in optParams)
                {
                    raw.Add(item);
                    if (item != optParams.Last())
                    {
                        raw.Add(" | ");
                    }
                    raw.Add(" ");
                }
                raw.Add(" = <any>{}");
                method.Parameters.Add(new FunctionParameter("opt") { ParameterType = new TypescriptTypeReference(raw) });
            }
        }

        protected virtual void GenerateNamedParametersValue(ActionModel action, FunctionMember method)
        {
            var namedParameters = action.Parameters.Where(x => !x.IsUrlParam && !x.IsData).ToArray();
            if (namedParameters.Length == 0)
            {
                method.Body.Statements.Add("{}");
                return;
            }
            //this optimalization cannot be done due to renaming params signature=>urlName
            //if (namedParameters.All(p => p.IsOptional))
            //{
            //    method.Body.Statements.Add("opt");
            //    return;
            //}
            var pilist = new List<string>();
            foreach (var p in namedParameters)
            {
                var pinvoke = $"'{(p.UrlName ?? p.Name)}': ";
                pinvoke += p.IsOptional ? ($"opt['{p.Name}']") : p.Name;
                pilist.Add(pinvoke);
            }
            method.Body.Statements.Add("{ ");
            method.Body.Statements.Add(String.Join(", ", pilist));
            method.Body.Statements.Add(" }");
        }

    }
}
