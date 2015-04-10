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
        }

        public List<ControllerModel> GetControllersModel(IEnumerable<Type> types)
        {
            var result = new List<ControllerModel>();
            foreach (var t in types)
            {
                var name = t.Name;
                if (name.EndsWith("Controller"))
                    name = name.Substring(0, name.Length - 10);
                var controllerPath = name.ToLower();
                var cModel = new ControllerModel { Source = t, Name = t.Name, Actions = GetActionsModel(t, controllerPath) };
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
                    HttpMethod = GetHttpMethod(m)
                };
                if (m.ReturnType != typeof(void))
                {
                    aModel.ResultType = m.ReturnType;
                }
                ProcessRoute(aModel, m, controllerPath);
                AddParameters(aModel, m);
                aModel.MethodName = m.Name + "Async";
                result.Add(aModel);
            }
            return result;
        }

        public void AddParameters(ActionModel a, MethodInfo m)
        {
            foreach (var mparam in m.GetParameters())
            {
                if (a.Parameters.Any(p => p.Name == mparam.Name))
                    continue;
                var pModel = new ParameterModel() { Name = mparam.Name, Type = mparam.ParameterType, IsOptional = mparam.IsOptional };
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
            return type.IsPrimitive || type.IsEnum || type == typeof(string);
        }

        public void ProcessRoute(ActionModel a, MethodInfo m, string controllerPath)
        {
            a.UrlTemplate = GetRouteTemplate(m);
            if (a.UrlTemplate != null)
            {
                //teoreticky lze pouzit DirectRouteBuilder, RouteParser, HttpParsedRoute, ale vse je internal :-(
                var parts = a.UrlTemplate.Split('/');
                foreach (var part in parts)
                {
                    if (part.StartsWith("{"))
                    {
                        var pname = part.Trim(new[] { '{', '}' });
                        var mparam = m.GetParameters().FirstOrDefault(p => p.Name == pname);
                        a.Parameters.Add(new ParameterModel() { Name = pname, Type = mparam.ParameterType, IsOptional = false, IsUrlParam = true });
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

        private static string GetActionName(MethodInfo m, string httpMethod)
        {
            var actionNameAt = m.GetCustomAttributes(true).FirstOrDefault(at => at.GetType().IsTypeBaseOrSelf("System.Web.Http.ActionNameAttribute"));
            if (actionNameAt != null)
            {
                return ((dynamic)actionNameAt).Name;
            }
            var actionName = m.Name;
            if (actionName.ToUpper().StartsWith(httpMethod))
            {
                actionName = actionName.Substring(httpMethod.Length);
            }
            return actionName;
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
            var proxyClass = new ClassType("GeneratedProxy");
            proxyClass.Extends = new TypescriptTypeReference("base.ProxyBase");
            targetModule.Members.Add(new DeclarationModuleElement(proxyClass) { IsExporting = true });
            foreach (var cm in controllers)
            {
                var cls = new ClassType(cm.Name + "Proxy");
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
                foreach (var am in cm.Actions)
                {
                    cls.Members.Add(GenerateAction(am, reflectionGenerator));
                }
                targetModule.Members.Add(cls);

                proxyClass.Members.Add(new PropertyMember(cm.Name.Replace("Controller", ""))
                {
                    MemberType = cls,
                    Accessibility = AccessibilityEnum.Public,
                    Initialization = new RawStatements("new ", cls, "(this)")
                });
            }

            //TODO: 
            /*
   WriteLine("import base = require('backend/ProxyBase');");
   WriteLine("export class GeneratedProxy extends base.ProxyBase {");
   PushIndent("\t");
   foreach (var x in controllers)
   {
       WriteLine(String.Format("public {0} : {1} = new {1}(this);", x.Name, x.Name));
   }
   PopIndent();
   WriteLine("}");
            */
        }

        private FunctionMember GenerateAction(ActionModel action, ReflectionGeneratorBase reflectionGenerator)
        {
            var method = new FunctionMember(action.Name + "Async", null) { Accessibility = AccessibilityEnum.Public };
            method.Parameters.AddRange(action.Parameters
                .Where(p => !p.IsOptional)
                .Select(p => new FunctionParameter(p.Name)
                {
                    ParameterType = reflectionGenerator.GenerateFromType(p.Type),
                    IsOptional = false
                }));
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
                method.ResultType = new TypescriptTypeReference("JQueryXHR");
            }
            method.Body = new RawStatements();
            method.Body.Statements.Add("return this._parent.call" + action.HttpMethod + "(");
            var urlVar = "'" + action.UrlTemplate.TrimEnd('/') + "'";
            foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
            {
                urlVar = urlVar.Replace("{" + p.Name + "}", "' + " + p.Name + " + '");
            }
            var EMPTYJS = " + ''";
            while (urlVar.EndsWith(EMPTYJS))
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
            }
            method.Body.Statements.Add(");");
            return method;
        }
        /*
   var plist = new List<string>();
   foreach (var p in action.Parameters.Where(p => !p.IsOptional))
   {
       plist.Add(p.Name + ": " + p.Type);
   }

   if (action.Parameters.Any(x => x.IsOptional))
   {
       plist.Add("opt: {" + String.Join("; ", polist) + "} = {}");
   }
   Write(String.Join(", ", plist));
   
        // call of parent
   PushIndent("\t");
   {
       Write("return this._parent.call" + action.HttpMethod);
       Write("(");

       var urlVar = "'" + action.UrlTemplate.TrimEnd('/') + "'";
       foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
       {
           urlVar = urlVar.Replace("{" + p.Name + "}", "' + " + p.Name + " + '");
       }
       var EMPTYJS = " + ''";
       while (urlVar.EndsWith(EMPTYJS))
       {
           urlVar = urlVar.Substring(0, urlVar.Length - EMPTYJS.Length);
       }

       Write(urlVar + ", {");
       var pilist = new List<string>();
       foreach (var p in action.Parameters.Where(x => !x.IsUrlParam && !x.IsData))
       {
           var pinvoke = "'" + p.Name + "': " + (p.IsOptional ? "opt." : "") + p.Name;
           pilist.Add(pinvoke);
       }

       Write(String.Join(",", pilist));
       Write("}");
       var dataParam = action.Parameters.FirstOrDefault(p => p.IsData);
       if (dataParam != null)
       {
           Write(", " + dataParam.Name);
       }
       WriteLine(");");
   }
   PopIndent();
   WriteLine("}");
}
*/

    }
}
