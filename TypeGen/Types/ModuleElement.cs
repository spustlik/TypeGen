namespace TypeGen
{
    public abstract class ModuleElement : TypeDomBase
    {
        public string Comment { get; set; }
        public bool IsExporting { get; set; }
        public static implicit operator ModuleElement(TypeDefType from)
        {
            return new DeclarationModuleElement(from);
        }
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
        public TypeDefType TypeDef { get; }
        public DeclarationBase Declaration { get; }
        public EnumType EnumDeclaration { get;  }
        public TypescriptModule InnerModule { get; }

        public DeclarationModuleElement(TypeDefType typeDef)
        {
            TypeDef = typeDef;
        }
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
            return (IsExporting ? "export " : "") + 
                (Declaration ?? EnumDeclaration ?? InnerModule ?? (object)TypeDef).ToString();
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
