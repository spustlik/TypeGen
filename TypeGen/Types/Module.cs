using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class Module : TypeDomBase
    {
        public string Name { get; set; }
        public List<ModuleElement> Members { get; private set; }
        public Module()
        {
            Members = new List<ModuleElement>();
        }
    }

    public abstract class ModuleElement : TypeDomBase
    {
        public bool IsExporting { get; set; }
        public static implicit operator ModuleElement(DeclarationBase from)
        {
            return new DeclarationModuleElement(from);
        }
        public static implicit operator ModuleElement(EnumType from)
        {
            return new DeclarationModuleElement(from);
        }
        public static implicit operator ModuleElement(Module from)
        {
            return new DeclarationModuleElement(from);
        }
        public static implicit operator ModuleElement(RawStatements from)
        {
            return new RawModuleElement() { Raw = from };
        }
    }

    public sealed class DeclarationModuleElement : ModuleElement
    {
        public DeclarationBase Declaration { get; private set; }
        public EnumType EnumDeclaration { get; private set; }
        public Module InnerModule { get; private set; }

        public DeclarationModuleElement(DeclarationBase decl)
        {
            Declaration = decl;
        }
        public DeclarationModuleElement(EnumType enumDecl)
        {
            EnumDeclaration = enumDecl;
        }
        public DeclarationModuleElement(Module innerModule)
        {
            InnerModule = innerModule;
        }

    }

    //used for 
    // - statement, 
    // - variable declaration
    // - function declaration
    // - typealias declaration
    // TODO: import declaration
    // TODO: ambient declaration
    public sealed class RawModuleElement : ModuleElement
    {
        public RawStatements Raw { get; set; }
    }
}
