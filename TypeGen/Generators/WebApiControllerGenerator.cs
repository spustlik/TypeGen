using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class WebApiControllerGenerator
    {
        public bool AddAsyncSuffix { get; set; } = true;

        public class ControllerModel
        {
            public Type Source { get; set; }
            public string Name { get; set; }
            public string Comment { get; set; }
            public List<ActionModel> Actions = new List<ActionModel>();
        }

        public class ActionModel
        {
            public MethodInfo Source { get; set; }
            public string Comment { get; set; }
            public string Name { get; set; }
            public string MethodName { get; set; }
            public string HttpMethod { get; set; }
            public string UrlTemplate { get; set; }
            public Type ResultType { get; set; }
            public List<ParameterModel> Parameters = new List<ParameterModel>();
        }

        public class ParameterModel
        {
            public ParameterInfo Source { get; set; }
            public string Name { get; set; }
            public string UrlName { get; set; }
            public Type Type { get; set; }
            public bool IsUrlParam { get; set; }
            public bool IsOptional { get; set; }
            public bool IsData { get; set; }
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(Name);
                if (!String.IsNullOrEmpty(UrlName))
                {
                    sb.Append(" (" + UrlName + ")");
                }
                if (IsUrlParam)
                    sb.Append(" [URL]");
                if (IsOptional)
                    sb.Append(" [OPT]");
                if (IsData)
                    sb.Append(" [DATA]");
                return sb.ToString();
            }
        }

        public List<ControllerModel> GetControllersModel(IEnumerable<Type> types)
        {
            var result = new List<ControllerModel>();
            foreach (var t in types)
            {
                var cModel = CreateControllerModel(t);
                result.Add(cModel);
            }
            return result;
        }

        public ControllerModel CreateControllerModel(Type t)
        {
            var name = t.Name;
            if (name.EndsWith("Controller", StringComparison.InvariantCulture))
                name = name.Substring(0, name.Length - 10);
            var controllerPath = GetRoutePrefix(t, name.ToLower());
            var cModel = new ControllerModel
            {
                Source = t,
                Name = t.Name,
                Actions = GetActionsModel(t, controllerPath),
                Comment = string.Format("class {0}, controllerPath={1}", t, controllerPath),
            };
            return cModel;
        }

        public List<ActionModel> GetActionsModel(Type t, string controllerPath)
        {
            var result = new List<ActionModel>();
            var methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                if (!IsControllerAction(m))
                    continue;
                var aModel = CreateActionModel(m, controllerPath);
                result.Add(aModel);
            }
            return result;
        }

        public ActionModel CreateActionModel(MethodInfo m, string controllerPath)
        {
            var aModel = new ActionModel()
            {
                Source = m,
                Name = m.Name,
                HttpMethod = GetHttpMethod(m),
                Comment = GetActionComment(m),
                ResultType = GetActionResultType(m)
            };
            ProcessRoute(aModel, m, controllerPath);
            AddParameters(aModel, m);
            if (aModel.HttpMethod == "GET")
                aModel.Parameters.ForEach(p => p.IsData = false);

            aModel.MethodName = m.Name;
            if (this.AddAsyncSuffix)
            {
                if (!aModel.MethodName.EndsWith("Async", StringComparison.Ordinal))
                    aModel.MethodName += "Async";
            }
            return aModel;
        }

        protected virtual string GetActionComment(MethodInfo m)
        {
            var sb = new StringBuilder();
            var route = GetRouteTemplate(m);
            if (!String.IsNullOrEmpty(route))
            {
                sb.Append("[Route(\"" + route + "\")]\n");
            }
            sb.Append(ReflectionHelper.MethodToString(m)+"\n");
            return sb.ToString();
        }

        private Type GetActionResultType(MethodInfo m)
        {
            var type = m.ReturnType;
            type = Helper.ExtractAsyncTaskType(type);
            if (type == typeof(void))
                return null;
            if (type.IsTypeBaseOrSelf("System.Net.Http.HttpResponseMessage"))
                return null;
            return type;
        }

        public void AddParameters(ActionModel a, MethodInfo m)
        {
            foreach (var mparam in m.GetParameters())
            {
                if (a.Parameters.Any(p => p.Name == mparam.Name || p.UrlName == GetMethodParameterName(mparam)))
                    continue;
                var pModel = new ParameterModel() { Name = mparam.Name, UrlName = GetMethodParameterName(mparam), Type = mparam.ParameterType, IsOptional = mparam.IsOptional };
                a.Parameters.Add(pModel);
                if (IsHttpBodyParam(mparam))
                {
                    pModel.IsData = true;
                }
                else
                {
                    if (!IsUrlParameter(pModel.Type))
                    {
                        if (a.Parameters.Any(p => p.IsData))
                        {
                            throw new InvalidOperationException(String.Format("Duplicate body parameter {2} ({3}), action: {0}, declaring type:{1}. ", a.Name, a.Source.DeclaringType, pModel.Name, a.Parameters.First(p => p.IsData).Name));
                        }
                        pModel.IsData = true;
                    }
                }
            }
        }

        private bool IsUrlParameter(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                return IsUrlParameter(Nullable.GetUnderlyingType(type));
            if (type.IsArray)
                return IsUrlParameter(type.GetElementType());
            return type.IsPrimitive || type.IsEnum || type == typeof(string);
        }

        public void ProcessRoute(ActionModel a, MethodInfo m, string controllerPath)
        {
            a.UrlTemplate = GetRouteTemplate(m);
            if (a.UrlTemplate != null)
            {
                if (!a.UrlTemplate.StartsWith("/", StringComparison.InvariantCulture))
                    a.UrlTemplate = controllerPath + "/" + a.UrlTemplate;
                //teoreticky lze pouzit DirectRouteBuilder, RouteParser, HttpParsedRoute, ale vse je internal :-(
                var parts = a.UrlTemplate.Split('/');
                foreach (var part in parts)
                {
                    if (part.StartsWith("{", StringComparison.InvariantCulture))
                    {
                        var pname = part.Trim(new[] { '{', '}' });
                        var mparam = m.GetParameters().FirstOrDefault(p => GetMethodParameterName(p) == pname);
                        if (mparam == null)
                        {
                            throw new Exception(string.Format("Cannot find parameter {0} of method {1} on controller {2} acquired from route '{3}'", pname, m.Name, m.ReflectedType.Name, a.UrlTemplate));
                        }
                        a.Parameters.Add(new ParameterModel() { Name = mparam.Name, UrlName = pname, Type = mparam.ParameterType, IsOptional = false, IsUrlParam = true });
                    }
                }
            }
            else
            {
                var actionPath = controllerPath;
                var actionName = GetActionName(m, a.HttpMethod);
                if (!String.IsNullOrEmpty(actionName))
                    actionPath = controllerPath + "/" + actionName;
                a.UrlTemplate = actionPath;
            }
        }

        private static bool IsControllerAction(MethodInfo m)
        {
            var nonActionAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.NonActionAttribute"));
            return nonActionAt == null;
        }

        private static string GetMethodParameterName(ParameterInfo p)
        {
            var fromUriAt = p.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.FromUriAttribute"));
            if (fromUriAt != null)
            {
                return ((dynamic)fromUriAt).Name ?? p.Name;
            }
            return p.Name;
        }

        private static string GetActionName(MethodInfo m, string httpMethod)
        {
            var actionNameAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.ActionNameAttribute"));
            if (actionNameAt != null)
            {
                return ((dynamic)actionNameAt).Name;
            }
            var actionName = m.Name;
            if (actionName.ToUpper().StartsWith(httpMethod, StringComparison.InvariantCulture))
            {
                actionName = actionName.Substring(httpMethod.Length);
            }
            return actionName;
        }

        private static bool IsHttpBodyParam(ParameterInfo pi)
        {
            return pi.GetCustomAttributes(false).Any(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.FromBodyAttribute"));
        }
        private static string GetRoutePrefix(MemberInfo m, string defaultValue)
        {
            var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.RoutePrefixAttribute"));
            if (routeAt != null)
            {
                return ((dynamic)routeAt).Prefix;
            }
            return defaultValue;
        }

        private static string GetRouteTemplate(MethodInfo m)
        {
            var routeAt = m.GetCustomAttributes(false).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.RouteAttribute"));
            if (routeAt != null)
            {
                return ((dynamic)routeAt).Template;
            }
            return null;
        }

        private static string GetHttpMethod(MethodInfo m)
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
            return "GET";
        }


        public void GenerateControllers(IEnumerable<ControllerModel> controllers, ReflectionGeneratorBase reflectionGenerator, TypescriptModule targetModule)
        {
            var proxyClass = new ClassType("GeneratedProxy")
            {
                Extends = new TypescriptTypeReference("base.ProxyBase")
            };
            targetModule.Members.Add(new DeclarationModuleElement(proxyClass) { IsExporting = true });
            foreach (var controller in controllers)
            {
                var cls = new ClassType(controller.Name + "Proxy");
                var proxyType = new TypescriptTypeReference("GeneratedProxy");
                cls.Members.Add(new PropertyMember("_parent")
                {
                    MemberType = proxyType,
                    Accessibility = AccessibilityEnum.Private
                });
                //cls.Members.Add(new RawStatements("constructor(parent: ", proxyType, ") {\n\tthis._parent = parent;\n}"));
                cls.Members.Add(new FunctionMember("constructor", new RawStatements("this._parent = parent;"))
                {
                    Accessibility = null,
                    Parameters = { new FunctionParameter("parent") { ParameterType = proxyType } },
                });
                foreach (var am in controller.Actions)
                {
                    cls.Members.Add(GenerateAction(am, reflectionGenerator));
                }
                targetModule.Members.Add(cls);
                targetModule.Members.Last().Comment = controller.Comment;

                proxyClass.Members.Add(new PropertyMember(controller.Name.Replace("Controller", ""))
                {
                    MemberType = cls,
                    Accessibility = AccessibilityEnum.Public,
                    Initialization = new RawStatements("new ", cls, "(this)")
                });
            }
            targetModule.Members.Add(new RawStatements("export var proxy = new ", proxyClass, "();"));
        }

        private FunctionMember GenerateAction(ActionModel action, ReflectionGeneratorBase reflectionGenerator)
        {
            var method = new FunctionMember(action.Name + "Async", null)
            {
                Accessibility = AccessibilityEnum.Public,
                Comment = "*" + action.Comment + "\n parameters: " + String.Join(", ", action.Parameters.Select(p => p.ToString())) + "\n"
            };
            GenerateMethodParametersSignature(action, method, reflectionGenerator);
            if (action.ResultType != null)
            {
                method.ResultType = new TypescriptTypeReference("JQueryPromise") { GenericParameters = { reflectionGenerator.GenerateFromType(action.ResultType) } };
            }
            else
            {
                method.ResultType = new TypescriptTypeReference("JQueryPromise") { GenericParameters = { PrimitiveType.Void } };
            }

            method.Body = new RawStatements();
            method.Body.Statements.Add("return this._parent.call" + action.HttpMethod + "(");
            GenerateUrlParametersValue(action, method);
            method.Body.Statements.Add(", ");
            GenerateNamedParametersValue(action, method);
            var dataParam = action.Parameters.FirstOrDefault(p => p.IsData);
            if (dataParam != null)
            {
                method.Body.Statements.Add(", " + dataParam.Name);
                if (dataParam.IsData)
                {
                    method.Body.Statements.Add("/* DATA */");
                }
            }
            method.Body.Statements.Add(");");
            return method;
        }

        private static void GenerateUrlParametersValue(ActionModel action, FunctionMember method)
        {
            var urlVar = "'" + action.UrlTemplate.TrimEnd('/') + "'";
            foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
            {
                urlVar = urlVar.Replace("{" + (p.UrlName ?? p.Name) + "}", "' + " + p.Name + " + '");
            }
            var EMPTYJS = " + ''";
            while (urlVar.EndsWith(EMPTYJS, StringComparison.InvariantCulture))
            {
                urlVar = urlVar.Substring(0, urlVar.Length - EMPTYJS.Length);
            }

            method.Body.Statements.Add(urlVar);
        }

        private static void GenerateMethodParametersSignature(ActionModel action, FunctionMember method, ReflectionGeneratorBase reflectionGenerator)
        {
            method.Parameters.AddRange(action.Parameters
                .Where(p => !p.IsOptional)
                .Select(p => new FunctionParameter(p.Name)
                {
                    ParameterType = reflectionGenerator.GenerateFromType(p.Type),
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
                    .Select(p => new RawStatements("{ ", p.Name, ": ", reflectionGenerator.GenerateFromType(p.Type), " }"))
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

        private static void GenerateNamedParametersValue(ActionModel action, FunctionMember method)
        {
            var namedParameters = action.Parameters.Where(x => !x.IsUrlParam && !x.IsData).ToArray();
            if (namedParameters.Length == 0)
            {
                method.Body.Statements.Add("{}");
                return;
            }
            if (namedParameters.All(p => p.IsOptional))
            {
                method.Body.Statements.Add("opt");
                return;
            }
            var pilist = new List<string>();
            foreach (var p in namedParameters)
            {
                var pinvoke = "'" + (p.UrlName ?? p.Name) + "': ";
                pinvoke += p.IsOptional ? ("opt['" + p.Name + "']") : p.Name;
                pilist.Add(pinvoke);
            }
            method.Body.Statements.Add("{ ");
            method.Body.Statements.Add(String.Join(", ", pilist));
            method.Body.Statements.Add(" }");
        }
    }
}
