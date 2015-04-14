using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Generators
{
    public class Optimizer : Visitors.RewriterBase
    {
        public static TypescriptModule RemoveEmptyDeclarations(TypescriptModule module, params DeclarationBase[] exceptions)
        {
            var o = new Optimizer();
            o.Exceptions.AddRange(exceptions);
            return o.RewriteModule(module);
        }

        public List<TypeDomBase> Exceptions { get; private set; }
        private Dictionary<DeclarationBase, DeclarationBase> _map = new Dictionary<DeclarationBase, DeclarationBase>();
        
        public Optimizer()
        {
            Exceptions = new List<TypeDomBase>();
        }

        public override TypescriptModule RewriteModule(TypescriptModule module)
        {
            var r = base.RewriteModule(module);
            var changed = true;
            while(changed)
            {
                changed = false;
                foreach (var m in r.Members.Where(IsEmptyDeclaration).ToArray())
                {
                    changed = true;
                    r.Members.Remove(m);
                }
            };
            return r;
        }

        public override DeclarationBase RewriteObjectReference(DeclarationBase type)
        {
            DeclarationBase result;
            if (_map.TryGetValue(type, out result))
            {
                return result; // some recursion needed?
            }
            result = base.RewriteObjectReference(type);
            if (IsEmptyDeclaration(result))
            {
                var newType = result.GetExtends().Select(x => x.ReferencedType).OfType<DeclarationBase>().FirstOrDefault();
                _map[result] = newType;
                return newType;
            }
            return result;
        }

        private bool IsEmptyDeclaration(ModuleElement me)
        {
            var e = me as DeclarationModuleElement;
            if (e == null)
                return false;
            if (e.Declaration != null)
            {
                return IsEmptyDeclaration(e.Declaration);
            }
            if (e.EnumDeclaration != null)
            {
                if (Exceptions.Contains(e.EnumDeclaration))
                    return false;
                return e.EnumDeclaration.Members.Count == 0;
            }
            return false;
        }

        private bool IsEmptyDeclaration(DeclarationBase e)
        {
            if (Exceptions.Contains(e))
                return false;
            return e.Members.Count == 0 && e.GetExtends().Count() <= 1;
        }

    }
}
