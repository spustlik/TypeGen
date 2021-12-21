using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators.WebApi
{
    public partial class WebApiModelReflection
    {
        public IWebApiReflection Reflection = new NetApiReflection();        
        public bool StripHttpMethodPrefixes { get; set; } = false;
        public bool SkipParamsCheck { get; set; } = false;
        public bool AddAsyncSuffix { get; set; } = true;
        public bool UseControlerNameWithoutRoutePrefix { get; set; } = true;


        public List<ControllerModel> GetControllersModel(IEnumerable<Type> types)
        {
            var result = new List<ControllerModel>();
            foreach (var t in types)
            {
                var cModel = CreateControllerModel(t);
                if (cModel != null)
                    result.Add(cModel);
            }
            return result;
        }

        public virtual ControllerModel CreateControllerModel(Type t)
        {
            var name = t.Name;
            if (name.EndsWith("Controller", StringComparison.InvariantCulture))
                name = name.Substring(0, name.Length - 10);
            var controllerPath = Reflection.GetRoutePrefixAttribute(t, UseControlerNameWithoutRoutePrefix  ? name.ToLower() : null);
            var cModel = new ControllerModel
            {
                Source = t,
                Name = t.Name,
                Description = Reflection.GetTypeGenDescription(t),
                Actions = GetActionsModel(t, controllerPath),
                Comment = string.Format("class {0}, controllerPath={1}", t, controllerPath),
            };
            return cModel;
        }

        public virtual List<ActionModel> GetActionsModel(Type t, string controllerPath)
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
                MethodName = GetActionMethodName(m),
                HttpMethod = Reflection.GetHttpMethodAttribute(m) ?? "GET",
                Comment = GetActionComment(m),
                Description = Reflection.GetTypeGenDescription(m),
                ResultType = GetActionResultType(m)
            };
            ProcessRoute(aModel, m, controllerPath);
            AddParameters(aModel, m);
            var dataParams = aModel.Parameters.Where(x => x.IsData).ToArray();
            if (aModel.HttpMethod == "GET")
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
                if (!SkipParamsCheck && dataParams.Length > 1)
                {
                    throw new Exception($"POST method {m.DeclaringType.Name}.{m.Name} cannot have more than 1 [FromBody] parameter");
                }
                if (!SkipParamsCheck && dataParams.Length == 0 && aModel.Parameters.Count(p => p.SourceType == ParamSourceType.MethodParam) > 0)
                {
                    throw new Exception($"POST method {m.DeclaringType.Name}.{m.Name} have parameters without specifying source, but no [FromBody] parameter. Mark it with [FromUri] or [FromBody]");
                }
            }
            return aModel;
        }

        protected virtual string GetActionMethodName(MethodInfo m)
        {
            var result = m.Name;
            if (this.AddAsyncSuffix)
            {
                if (!result.EndsWith("Async", StringComparison.Ordinal))
                    result += "Async";
            }
            return result;
        }

        protected virtual string GetActionComment(MethodInfo m)
        {
            var lines = new List<String>();
            var route = Reflection.GetRouteTemplateAttribute(m);
            if (!String.IsNullOrEmpty(route))
            {
                lines.Add($"[Route(\"{route}\")]");
            }
            lines.Add(ReflectionHelper.MethodToString(m, useFullTypeName: false));
            return String.Join("\n", lines);
        }

        protected virtual Type GetActionResultType(MethodInfo m)
        {
            var type = m.ReturnType;
            type = Helper.ExtractAsyncTaskType(type);
            if (type == typeof(void))
                return null;
            if (Reflection.IsHttpResponseMessage(type))
                return null;
            return type;
        }


        public void AddParameters(ActionModel a, MethodInfo m)
        {
            foreach (var mparam in m.GetParameters())
            {
                if (a.Parameters.Any(p => p.Name == mparam.Name || p.UrlName == GetMethodParameterName(mparam)))
                    continue;
                var pModel = CreateMethodParameter(a, mparam);
                if (pModel != null)
                    a.Parameters.Add(pModel);
            }
        }

        protected virtual ParameterModel CreateMethodParameter(ActionModel a, ParameterInfo mparam)
        {
            var pModel = new ParameterModel()
            {
                Source = mparam,
                SourceType = ParamSourceType.MethodParam,
                Name = mparam.Name,
                UrlName = GetMethodParameterName(mparam),
                Type = mparam.ParameterType,
                IsOptional = mparam.IsOptional
            };
            if (Reflection.HasHttpBodyParamAttribute(mparam))
            {
                pModel.SourceType = ParamSourceType.MethodParamBody;
                pModel.IsData = true;
            }
            else if (Reflection.GetFromUriAttribute(mparam, out _))
            {
                pModel.SourceType = ParamSourceType.MethodParamUri;
            }
            else if (!CanBeInUrl(pModel.Type))
            {
                if (a.Parameters.Any(p => p.IsData))
                {
                    throw new InvalidOperationException(String.Format("Duplicate body parameter {2} ({3}), action: {0}, declaring type:{1}. ", a.Name, a.Source.DeclaringType, pModel.Name, a.Parameters.First(p => p.IsData).Name));
                }
                pModel.IsData = true;
            }
            return pModel;
        }

        protected virtual bool CanBeInUrl(Type type)
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
            if (a.UrlTemplate == null)
            {
                var actionPath = controllerPath;
                var actionName = GetActionName(m, a.HttpMethod);
                if (!String.IsNullOrEmpty(actionName))
                    actionPath = CombinePath(controllerPath, actionName);
                a.UrlTemplate = actionPath;
                return;
            }
            if (!a.UrlTemplate.StartsWith("/", StringComparison.InvariantCulture))
                a.UrlTemplate = CombinePath(controllerPath, a.UrlTemplate);
            //theoretically, DirectRouteBuilder, RouteParser, HttpParsedRoute can be used, but it is marked as internal :-(
            var parts = a.UrlTemplate.Split('/');

            foreach (var part in parts)
            {
                if (part.StartsWith("{", StringComparison.InvariantCulture))
                {
                    var pmodel = CreateTemplateParameter(a, m, part);
                    if (pmodel != null)
                        a.Parameters.Add(pmodel);
                }
            }
        }

        private string CombinePath(string p1, string p2)
        {
            if (String.IsNullOrEmpty(p1))
                return p2;
            if (String.IsNullOrEmpty(p2))
                return p1;
            return p1 + "/" + p2;
        }

        protected virtual ParameterModel CreateTemplateParameter(ActionModel a, MethodInfo m, string part)
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

            return pmodel;
        }

        protected virtual bool IsControllerAction(MethodInfo m)
        {
            return !Reflection.HasNonActionAttribute(m);
        }

        protected virtual string GetMethodParameterName(ParameterInfo p)
        {
            if (Reflection.GetFromUriAttribute(p, out var name))
                return name ?? p.Name;
            return p.Name;
        }

        protected virtual string GetActionName(MethodInfo m, string httpMethod)
        {
            var actionName = m.Name;
            if (this.StripHttpMethodPrefixes)
            {
                if (actionName.ToUpper().StartsWith(httpMethod, StringComparison.InvariantCulture))
                {
                    actionName = actionName.Substring(httpMethod.Length);
                }
            }
            return actionName;
        }


    }
    public class ControllerModel
    {
        public Type Source { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }

        public List<ActionModel> Actions = new List<ActionModel>();
    }

    public class ActionModel
    {
        public MethodInfo Source { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);
            if (!String.IsNullOrEmpty(UrlName) && UrlName!=Name)
                sb.Append(" (" + UrlName + ")");
            if (IsUrlParam)
                sb.Append(" [URL]");
            if (IsOptional)
                sb.Append(" [OPT]");
            if (IsData)
                sb.Append(" [DATA]");
            return sb.ToString();
        }
    }

    
}
