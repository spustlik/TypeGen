﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    [Obsolete("Use WebApi.WebApiModelReflection and WebApi.WebApiProxyGenerator")]
    public class WebApiControllerGenerator
    {
        public string PromiseTypeName = "Promise";
        public string GeneratedClassName = "GeneratedProxy";
        public string ProxyBaseName = "base.ProxyBase";

        public bool AddAsyncSuffix { get; set; } = true;
        public bool StripHttpMethodPrefixes { get; set; } = false;
        public bool SkipParamsCheck { get; set; } = false;

        public WebApi.IWebApiReflection Reflection = new WebApi.NetApiReflection();


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

        public enum ParamSourceType
        {
            Template,
            MethodParam,
            MethodParamBody,
            MethodParamUri,
        }
        public class ParameterModel
        {
            public ParameterInfo Source { get; set; }
            public ParamSourceType SourceType { get; set; }
            public string Name { get; set; }
            public string UrlName { get; set; }
            public Type Type { get; set; }
            public bool IsUrlParam { get; set; }
            public bool IsOptional { get; set; }
            public bool IsData { get; set; }
			public bool CanBeInUrl { get; set; }

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
            var controllerPath = Reflection.GetRoutePrefixAttribute(t, name.ToLower());
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
                if (Reflection.HasNonActionAttribute(m))
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
                HttpMethod = Reflection.GetHttpMethodAttribute(m) ?? "GET",
                Comment = GetActionComment(m),
                ResultType = GetActionResultType(m)
            };
            ProcessRoute(aModel, m, controllerPath);
            AddParameters(aModel, m);
            var dataParams = aModel.Parameters.Where(x => x.IsData).ToArray();
            if (aModel.HttpMethod == "GET" )
            {
                if (!SkipParamsCheck && dataParams.Length > 0)
                {
                    throw new Exception($"GET method {m.DeclaringType.Name}.{m.Name} cannot have [FromBody] parameter");
                }
                foreach (var p in dataParams)
                {
                    p.IsData = false;
                }
            }
            if (aModel.HttpMethod == "POST")
            {
                if (!SkipParamsCheck && dataParams.Length>1)
                {
                    throw new Exception($"POST method {m.DeclaringType.Name}.{m.Name} cannot have more than 1 [FromBody] parameter");
                }
                if (!SkipParamsCheck && dataParams.Length == 0 && aModel.Parameters.Count(p => p.SourceType == ParamSourceType.MethodParam && !p.CanBeInUrl) > 0)
                {
					throw new Exception(
						$"POST method {m.DeclaringType.Name}.{m.Name} have parameters without specifying source, but no [FromBody] parameter.\n" +
						$"Mark it with [FromUri] or [FromBody]\n" +
						$"Params = {String.Join(", ", aModel.Parameters.Select(p => $"{p.Name}[CanBeInUrl={p.CanBeInUrl}]"))}");
                }
            }
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
            var route = Reflection.GetRouteTemplateAttribute(m);
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
                var pModel = new ParameterModel()
                {
                    Source = mparam,
                    SourceType = ParamSourceType.MethodParam,
                    Name = mparam.Name,
                    UrlName = GetMethodParameterName(mparam),
                    Type = mparam.ParameterType,
                    IsOptional = mparam.IsOptional
                };
                a.Parameters.Add(pModel);
                if (Reflection.HasHttpBodyParamAttribute(mparam))
                {
                    pModel.SourceType = ParamSourceType.MethodParamBody;
                    pModel.IsData = true;
                }
                else if (Reflection.GetFromUriAttribute(mparam, out _))
                {
                    pModel.SourceType = ParamSourceType.MethodParamUri;
                }
                else 
                {
					pModel.CanBeInUrl = CanBeInUrl(pModel.Type);
					if (!pModel.CanBeInUrl)
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

        private bool CanBeInUrl(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                return CanBeInUrl(Nullable.GetUnderlyingType(type));
            if (type.IsArray)
                return CanBeInUrl(type.GetElementType());
            return type.IsPrimitive || type.IsEnum || type == typeof(string);
        }

        public void ProcessRoute(ActionModel a, MethodInfo m, string controllerPath)
        {
            a.UrlTemplate = Reflection.GetRouteTemplateAttribute(m);
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
                        var isOptional = pname.EndsWith("?", StringComparison.Ordinal);
                        if (isOptional)
                            pname = pname.TrimEnd('?');
                        var mparam = m.GetParameters().FirstOrDefault(p => GetMethodParameterName(p) == pname);
                        if (mparam == null)
                        {
                            throw new Exception(string.Format("Cannot find parameter {0} of method {1} on controller {2} acquired from route '{3}'", pname, m.Name, m.ReflectedType.Name, a.UrlTemplate));
                        }
                        var pmodel = new ParameterModel()
                        {
                            Source = mparam,
                            SourceType = ParamSourceType.Template,
                            Name = mparam.Name,
                            UrlName = pname,
                            Type = mparam.ParameterType,
                            IsOptional = isOptional || mparam.HasDefaultValue || mparam.IsOptional,
                            IsUrlParam = true
                        };
                        if (pmodel.IsOptional)
                        {
                            throw new Exception(String.Format("Optional url parameters are not supported: parameter {0} of method {1} on controller {2}", pname, m.Name, m.ReflectedType.Name));
                        }
                        a.Parameters.Add(pmodel);
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

        private string GetMethodParameterName(ParameterInfo p)
        {
            if (Reflection.GetFromUriAttribute(p, out var name))
                return name;
            return p.Name;
        }

        private string GetActionName(MethodInfo m, string httpMethod)
        {
            var actionName = Reflection.GetActionNameAttribute(m);
            if (!String.IsNullOrEmpty(actionName))
                return actionName;
            actionName = m.Name;
            if (this.StripHttpMethodPrefixes)
            {
                if (actionName.ToUpper().StartsWith(httpMethod, StringComparison.InvariantCulture))
                {
                    actionName = actionName.Substring(httpMethod.Length);
                }
            }
            return actionName;
        }


        public void GenerateControllers(IEnumerable<ControllerModel> controllers, ReflectionGeneratorBase reflectionGenerator, TypescriptModule targetModule)
        {
            var proxyClass = new ClassType("GeneratedProxy");
            if (!String.IsNullOrEmpty(ProxyBaseName))
            {
                proxyClass.Extends = new TypescriptTypeReference(ProxyBaseName);
            }
            targetModule.Members.Add(new DeclarationModuleElement(proxyClass) { IsExporting = true });
            foreach (var controller in controllers)
            {
                var cls = new ClassType(controller.Name + "Proxy");
                var proxyType = new TypescriptTypeReference(GeneratedClassName);
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
                Comment = GenerateActionComment(action)
            };
            GenerateMethodParametersSignature(action, method, reflectionGenerator);
            if (action.ResultType != null)
            {
                method.ResultType = new TypescriptTypeReference(PromiseTypeName) { GenericParameters = { reflectionGenerator.GenerateFromType(action.ResultType) } };
            }
            else
            {
                method.ResultType = new TypescriptTypeReference(PromiseTypeName) { GenericParameters = { PrimitiveType.Void } };
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

        private static string GenerateActionComment(ActionModel action)
        {
            var parameters = action.Parameters;
            //.Select(p => p.ToString() + " ( " + p.Source.Attributes + " - " + string.Join("|", p.Source.CustomAttributes) +")");
            return "*" + action.Comment + "\n" + 
                   " parameters: " + String.Join(", ", parameters) + "\n";
        }

        private static void GenerateUrlParametersValue(ActionModel action, FunctionMember method)
        {
            var urlVar = "'" + action.UrlTemplate.TrimEnd('/') + "'";
            foreach (var p in action.Parameters.Where(x => x.IsUrlParam))
            {
                var paramName = p.UrlName ?? p.Name;
                urlVar = urlVar.Replace("{" + paramName + "}", "' + " + p.Name + " + '");
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
            //this optimalization cannot be done due to renaming params signature=>urlName
            //if (namedParameters.All(p => p.IsOptional))
            //{
            //    method.Body.Statements.Add("opt");
            //    return;
            //}
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
