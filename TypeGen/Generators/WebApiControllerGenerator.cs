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
                result.Add(cModel);
            }
            return result;
        }

        public List<ActionModel> GetActionsModel(Type t, string controllerPath)
        {
            var result = new List<ActionModel>();
            var methods = t.GetMethods(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var m in methods)
            {
                var aModel = new ActionModel()
                {
                    Source = m,
                    Name = m.Name,
                    HttpMethod = GetHttpMethod(m),
                    Comment = "method " + m,
                    ResultType = GetActionResultType(m)
                };
                ProcessRoute(aModel, m, controllerPath);
                AddParameters(aModel, m);
                if (aModel.HttpMethod == "GET")
                    aModel.Parameters.ForEach(p => p.IsData = false);
                aModel.MethodName = m.Name + "Async";
                result.Add(aModel);
            }
            return result;
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
                if (!IsUrlParameter(pModel.Type))
                {
                    if (a.Parameters.Any(p => p.IsData))
                    {
                        throw new InvalidOperationException(String.Format("Duplicate body parameter {2} ({3}), action: {0}, declaring type:{1}. ", a.Name, a.Source.DeclaringType, pModel.Name, a.Parameters.First(p=>p.IsData).Name));
                    }
                    pModel.IsData = true;
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
            var method = new FunctionMember(action.Name + "Async", null) { Accessibility = AccessibilityEnum.Public, Comment = action.Comment };
            method.Comment+="\n parameters: " + String.Join(", ", action.Parameters.Select(p => p.ToString()));
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
            //  but, is is needed then to call it with positional params (TypeScript doesn't know named params)
            //  xxx.myMethod("asd","qwe",...,null, 42)
            //compare to: call via optional properties of anonymous object
            //  xxx.myMethod("asd","qwe",..., { po2: 42} )
            // BUT, with current version of typescript (1.4), there is bug?, because it can be called with another object
            //  xxx.myMethod("asd","qwe",..., "nonsense" )
            if (action.Parameters.Any(p => p.IsOptional))
            {
                var optParams = action.Parameters
                    .Where(p => p.IsOptional)
                    .Select(p => new RawStatements(p.Name, "?: ", reflectionGenerator.GenerateFromType(p.Type)))
                    .ToArray();
                var raw = new RawStatements();
                raw.Add("{ ");
                foreach (var item in optParams)
                {
                    raw.Add(item);
                    if (item != optParams.Last())
                    {
                        raw.Add(";");
                    }
                    raw.Add(" ");
                }
                raw.Add("} = {}");
                method.Parameters.Add(new FunctionParameter("opt") { ParameterType = new TypescriptTypeReference(raw) });
            }
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
            var urlVar = "'" + action.UrlTemplate.TrimEnd('/') + "'";
            foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
            {
                urlVar = urlVar.Replace("{" + (p.UrlName??p.Name) + "}", "' + " + p.Name + " + '");
            }
            var EMPTYJS = " + ''";
            while (urlVar.EndsWith(EMPTYJS, StringComparison.InvariantCulture))
            {
                urlVar = urlVar.Substring(0, urlVar.Length - EMPTYJS.Length);
            }

            method.Body.Statements.Add(urlVar);
            method.Body.Statements.Add(", {");            
            var pilist = new List<string>();
            foreach (var p in action.Parameters.Where(x => !x.IsUrlParam && !x.IsData))
            {
                var pinvoke = "'" + p.Name + "': " + (p.IsOptional ? "opt." : "") + p.Name;
                pilist.Add(pinvoke);
            }
            if (pilist.Count > 0)
            {
                method.Body.Statements.Add(" ");
                method.Body.Statements.Add(String.Join(", ", pilist));
                method.Body.Statements.Add(" ");
            }
            method.Body.Statements.Add("}");
            var dataParam = action.Parameters.FirstOrDefault(p => p.IsData);
            if (dataParam != null)
            {
                method.Body.Statements.Add(", " + dataParam.Name);
                method.Body.Statements.Add("/* " + dataParam.IsData + "*/");
            }
            method.Body.Statements.Add(");");
            return method;
        }

    }
}
