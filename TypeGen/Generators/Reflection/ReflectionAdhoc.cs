using System.Linq;
using System.Reflection;

namespace TypeGen.Generators.Reflection
{

    public abstract class ReflectionAdhocWrap
    {
        public string FullAttributeName { get; }
        protected ReflectionAdhocWrap(string fullName)
        {
            FullAttributeName = fullName;
        }
    }
}
