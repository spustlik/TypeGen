using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeGen
{
    /// <summary>
    /// name resolvr holding map to of modules aliases and cache of name in module for for each type
    /// </summary>
    /// can be improved when ThisModule is x.y and generating x.y.z.w then create only z.w
    public class AliasNameResolver : INameResolver
    {
        private Dictionary<string, TypescriptModule> _modules = new Dictionary<string, TypescriptModule>();
        private Dictionary<TypescriptTypeBase, string> _cache = new Dictionary<TypescriptTypeBase, string>();

        public TypescriptModule ThisModule
        {
            get { _modules.TryGetValue("", out var result); return result; }
            set { _modules[""] = value; _cache.Clear(); }
        }
        public bool EnableFails { get; set; }

        public void AddModule(TypescriptModule m, string alias = null)
        {
            _modules[alias ?? m.Name] = m;
        }
        private static bool ContainsItem<T>(TypescriptModule m, T item) where T : class
        {
            return m.Members
                .OfType<DeclarationModuleElement>()
                .Any(d => d.Declaration == item || d.EnumDeclaration == item || d.TypeDef == item);
        }

        private Tuple<string, TypescriptModule> FindModule<T>(T item) where T : class
        {
            if (ThisModule != null && ContainsItem(ThisModule, item))
            {
                return Tuple.Create((string)null, ThisModule);
            }
            foreach (var alias in _modules)
            {
                if (ContainsItem(alias.Value, item))
                    return Tuple.Create(alias.Key, alias.Value);
            }
            return null;
        }
        string INameResolver.GetReferencedName(DeclarationBase type) => GetRefName(type, x => x.Name);

        string INameResolver.GetReferencedName(EnumType type) => GetRefName(type, x => x.Name);

        string INameResolver.GetReferencedName(TypeDefType type) => GetRefName(type, x => x.Name);

        private string GetRefName<T>(T type, Func<T, string> getName) where T : TypescriptTypeBase
        {
            if (_cache.TryGetValue(type, out string result))
                return result;
            var m = FindModule(type);
            if (m != null)
            {
                var sb = new StringBuilder();
                sb.Append(m.Item1);
                if (!String.IsNullOrEmpty(m.Item1))
                    sb.Append(".");
                sb.Append(getName(type));
                result = sb.ToString();
            }
            else
            {
                result = GetFailedName(getName(type));
            }
            _cache[type] = result;
            return result;
        }
        protected virtual string GetFailedName(string name)
        {
            if (EnableFails)
                return $"FAILED<{name}>";
            return name;
        }

    }
}
