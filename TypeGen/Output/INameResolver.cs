using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    /// <summary>
    /// interface for ressolving externaly referenced types (for example from one module to another)
    /// </summary>
    public interface INameResolver
    {
        string GetReferencedName(DeclarationBase type);
        string GetReferencedName(EnumType type);
        string GetReferencedName(TypeDefType type);
    }

    /// <summary>
    /// simple name ressolver with module alias dictionary
    /// </summary>
    public class DefaultNameResolver : INameResolver
    {
        #region private static part
        private static Dictionary<string, TypescriptModule> Modules = new Dictionary<string, TypescriptModule>();
        private static Dictionary<TypescriptTypeBase, string> _cache = new Dictionary<TypescriptTypeBase, string>();

        private static bool ContainsItem<T>(TypescriptModule m, T item) where T : class
        {
            return m.Members.OfType<DeclarationModuleElement>().Any(d => d.Declaration == item || d.EnumDeclaration == item);
        }

        private static Tuple<string, TypescriptModule> FindModule<T>(T item) where T : class
        {
            if (ThisModule != null && ContainsItem(ThisModule, item))
            {
                return Tuple.Create((string)null, ThisModule);
            }
            foreach (var alias in Modules)
            {
                if (ContainsItem(alias.Value, item))
                    return Tuple.Create(alias.Key, alias.Value);
            }
            return null;
        }

        #endregion

        public static TypescriptModule ThisModule
        {
            get { TypescriptModule result; Modules.TryGetValue("", out result); return result; }
            set { Modules[""] = value; _cache.Clear(); }
        }

        public static void AddModule(TypescriptModule m, string alias = null)
        {
            Modules[alias ?? m.Name] = m;
        }


        public string GetReferencedName(EnumType type)
        {
            if (_cache.TryGetValue(type, out string result))
                return result;
            var m = FindModule(type);
            if (m != null)
            {
                result = m.Item1;
                if (!String.IsNullOrEmpty(result))
                    result = result + ".";
                result = result + type.Name;
            }
            else
            {
                result = GetFailedName(type.Name);
            }
            _cache[type] = result;
            return result;
        }

        public string GetReferencedName(DeclarationBase type)
        {
            return GetRefName(type, x=>x.Name);
        }
        public string GetReferencedName(TypeDefType type)
        {
            return GetRefName(type, x=>x.Name);
        }

        private string GetRefName<T>(T type, Func<T,string> getName) where T:TypescriptTypeBase
        {
            if (_cache.TryGetValue(type, out string result))
                return result;
            var m = FindModule(type);
            var sb = new StringBuilder();
            if (m != null)
            {
                sb.Append(m.Item1);
                if (!String.IsNullOrEmpty(m.Item1))
                    sb.Append(".");
                sb.Append(getName(type));
            }
            else
            {
                sb.Append(GetFailedName(getName(type)));
            }
            result = sb.ToString();
            _cache[type] = result;
            return result;
        }

        public static bool EnableFails { get; set; }
        protected virtual string GetFailedName(string name)
        {
            if (EnableFails)
            {
                return "FAILED<" + name + ">";
            }
            return name;
        }

    }
}
