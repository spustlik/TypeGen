using System.Diagnostics;
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
}
