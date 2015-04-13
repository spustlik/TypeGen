using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public sealed class TypescriptModule : TypeDomBase
    {
        public string Comment { get; set; }
        public string Name { get; set; }
        public List<ModuleElement> Members { get; private set; }
        public TypescriptModule(string name)
        {
            Name = name;
            Members = new List<ModuleElement>();
        }

        public void MakeAllExportable(bool isExporting)
        {
            foreach (var element in Members)
            {
                element.IsExporting = isExporting;
            }
        }

        public override string ToString()
        {
            return "module " + Name + " (" + Members.Count + " members)";
        }

    }

    public abstract class ModuleElement : TypeDomBase
    {
        public string Comment { get; set; }
        public bool IsExporting { get; set; }
        public static implicit operator ModuleElement(DeclarationBase from)
        {
            return new DeclarationModuleElement(from);
        }
        public static implicit operator ModuleElement(EnumType from)
        {
            return new DeclarationModuleElement(from);
        }
        public static implicit operator ModuleElement(TypescriptModule from)
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
        public TypescriptModule InnerModule { get; private set; }

        public DeclarationModuleElement(DeclarationBase decl)
        {
            Declaration = decl;
        }
        public DeclarationModuleElement(EnumType enumDecl)
        {
            EnumDeclaration = enumDecl;
        }
        public DeclarationModuleElement(TypescriptModule innerModule)
        {
            InnerModule = innerModule;
        }

        public override string ToString()
        {
            return (IsExporting ? "export " : "") + ((object)Declaration ?? (object)EnumDeclaration ?? InnerModule).ToString();
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

        public override string ToString()
        {
            return Raw + "";
        }
    }
}
