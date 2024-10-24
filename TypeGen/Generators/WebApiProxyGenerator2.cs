using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypeGen.Generators.WebApi
{
    public class WebApiProxyGenerator2
    {
        protected readonly ReflectionGeneratorBase _reflectionGenerator;
        public string PromiseTypeName = "Promise";
        public string GeneratedClassName = "GeneratedProxy";
        public string ProxyBaseName = "base.ProxyBase";
        public string CallInstance = "this.";
        public bool UseOptionalRecord = true;
        public bool UseJsTemplates = true;
        public bool UseDirectParams = true;
        public bool UseOptParamsShortcut = true;
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
            fn.Body.Statements.Add($"{CallInstance}call{action.HttpMethod}(");
            GenerateUrlWithParameters(action, fn);
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

        protected virtual void GenerateUrlWithParameters(ActionModel action, FunctionMember method)
        {
            //replaces {paramName} in UrlTemplate like '/urlbase' + paramName + '/more/' + param2
            if (!UseJsTemplates)
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
                return;
            }

            //using js template strings like `/api/${id}`
            var r = new Regex(@"{[^}]+}", RegexOptions.Compiled);
            var url = action.UrlTemplate.TrimEnd('/');
            method.Body.Statements.Add("`");
            foreach (var part in r.SplitToParts(url))
            {
                var name = part;
                if (name.StartsWith("{"))
                {
                    name = name.Trim('{', '}');
                    var parameter = action.Parameters.FirstOrDefault(p => p.IsUrlParam && (p.UrlName ?? p.Name) == name);
                    if (parameter != null)
                    {
                        method.Body.Statements.Add("${" + parameter.Name + "}");
                        continue;
                    }
                }
                method.Body.Statements.Add(part);
            }
            method.Body.Statements.Add("`");
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
            var raw = GenerateOptionalParameters(action, method);
            if (raw != null)
            {
                method.Parameters.Add(new FunctionParameter("opt")
                {
                    ParameterType = new TypescriptTypeReference(raw)
                });
            }
        }

        private RawStatements GenerateOptionalParameters(ActionModel action, FunctionMember method)
        {
            //consider: 
            //  if there is only one optional parameter, or all opt. parameters are last (it must be in c# decl), 
            //  we can generate myMethod(p1,p2,..., po1:string? = null, po2:number? = null)
            //  but, it is needed then to call it with positional params (TypeScript doesn't know named params)
            //  xxx.myMethod("asd","qwe",...,null, 42)
            //compare to: call via optional properties of anonymous object
            //  xxx.myMethod("asd","qwe",..., { po2: 42} )

            // this is not true for now (2024-10-24):
            // BUT, it can be called with any object, because all positional parameters are optional, so any object will match
            //  xxx.myMethod("asd","qwe",..., "nonsense" )
            // partially solved:
            //  positional parameters are unions of non-nullable parameter objects (see code)
            //  but this approach will still validate only one of parameters, others are then ignored (because at least one matched)
            var optional = action.Parameters.Where(p => p.IsOptional).ToArray();
            if (optional.Length == 0)
                return null;

            if (!UseOptionalRecord)
            {
                var rawParams = optional
                    .Select(p => new RawStatements("{ ", p.Name, "?: ", _reflectionGenerator.GenerateFromType(p.Type), " }"))
                    .ToArray();
                var raw = RawStatements.Join(rawParams, ", ");
                return raw;
            }
            else
            {
                RawStatements createParam(ParameterModel p) => new RawStatements(p.Name, "?: ", _reflectionGenerator.GenerateFromType(p.Type));
                var rawParams = RawStatements.Join(optional.Select(p => createParam(p)), ", ");
                var raw = new RawStatements();
                raw.Add("{ ");
                raw.Add(rawParams.Statements);
                raw.Add(" }");
                raw.Add(" = {}");
                return raw;
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
            if (!UseDirectParams)
            {
                foreach (var p in namedParameters)
                {
                    var uname = QuoteIdent(p.UrlName ?? p.Name);
                    var pname = p.Name;

                    var pinvoke = uname;
                    //optimized { ident } instead of { ident: ident }
                    if (p.IsOptional || pname != uname)
                    {
                        pinvoke += ": ";
                        pinvoke += p.IsOptional ? ($"opt['{pname}']") : pname;
                    }
                    pilist.Add(pinvoke);
                }
            }
            else
            {
                bool rename = false;
                foreach (var p in namedParameters)
                {
                    var n1 = QuoteIdent(p.UrlName ?? p.Name);
                    var n2 = p.Name;
                    rename = rename || n1 != n2;
                    if (p.IsOptional)
                    {
                        n2 = $"opt.{n2}";
                    }
                    if (n2 != n1)
                    {
                        n1 = $"{n1}: {n2}";
                    }
                    pilist.Add(n1);
                }
                if (UseOptParamsShortcut && !rename && namedParameters.All(p => p.IsOptional))
                {
                    method.Body.Statements.Add("opt");
                    return;
                }
            }
            method.Body.Statements.Add("{ ");
            method.Body.Statements.Add(String.Join(", ", pilist));
            method.Body.Statements.Add(" }");
        }

        private string QuoteIdent(string ident)
        {
            if (ident.All(c => Char.IsLetterOrDigit(c)))
            {
                return ident;
            }
            return "'" + ident + "'";
        }
    }
}
