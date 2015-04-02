using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{    
    [DebuggerDisplay("{Formatter.DebuggerOutput}")]
    public class OutputGenerator
    {
        public TextFormatter Formatter { get; set; }
        public string Output { get { return Formatter.Output.ToString(); } }
        public OutputGenerator()
        {
            Formatter = new TextFormatter();
        }

        public void Generate(EnumType type)
        {
            Formatter.Write("enum ");
            Formatter.Write(type.Name);
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            Formatter.WriteSeparated(",\n", type.Members, Generate);
            Formatter.WriteLine();
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        private void Generate(EnumMember m)
        {
            Formatter.Write(m.Name);
            if (m.Value != null)
            {
                Formatter.Write(" = ");
                Formatter.Write(m.IsHexLiteral ? String.Format("0x{0:X}", m.Value.Value) : String.Format("{0}", m.Value.Value));
            }
        }

        public void Generate(DeclarationBase decl)
        {
            if (decl is ClassType)
            {
                Generate((ClassType)decl);
            }
            else if (decl is InterfaceType)
            {
                Generate((InterfaceType)decl);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public void Generate(ClassType cls)
        {
            Formatter.Write("class ");
            Formatter.Write(cls.Name);
            if (cls.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", cls.GenericParameters, Generate);
                Formatter.Write(">");
            }
            if (cls.IsExtending)
            {
                Formatter.Write(" extends ");
                Formatter.WriteSeparated(", ", cls.ExtendsTypes, Generate);
            }
            if (cls.IsImplementing)
            {
                Formatter.Write(" implements ");
                Formatter.WriteSeparated(", ", cls.Implementations, Generate);
            }
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            foreach (var m in cls.Members)
            {                
                Generate(m);
                //TODO: methods and ctor doesnt end with ";"
                Formatter.Write(";");
                Formatter.WriteLine();
            }
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        public void Generate(InterfaceType cls)
        {
            Formatter.Write("interface ");
            Formatter.Write(cls.Name);
            if (cls.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", cls.GenericParameters, Generate);
                Formatter.Write(">");
            }
            if (cls.IsExtending)
            {
                Formatter.Write(" extends ");
                Formatter.WriteSeparated(", ", cls.ExtendsTypes, Generate);
            }
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            foreach (var m in cls.Members)
            {
                Generate(m);                
                Formatter.Write(";"); //methods also
                Formatter.WriteLine();
            }
            Formatter.PopIndent();
            Formatter.Write("}");
        }

        private void Generate(DeclarationMember m)
        {
            if (m is PropertyMember)
            {
                Generate((PropertyMember)m);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(PropertyMember m)
        {
            if (m.Accessibility != null)
            {
                Formatter.Write(m.Accessibility.Value.ToStr());
                Formatter.Write(" ");
            }
            Formatter.Write(m.Name);
            if (m.IsOptional)
                Formatter.Write("?");
            Formatter.Write(": ");
            Generate(m.MemberType);            
        }

        private void Generate(TypescriptTypeReference obj)
        {
            if (!String.IsNullOrEmpty(obj.TypeName))
            {
                Formatter.Write(obj.TypeName);
            }
            else
            {
                //TODO: module qualified names, generic instances
                GenerateReference(obj.ReferencedType);
            }
            if (obj.GenericParameters.Count > 0)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", obj.GenericParameters, Generate);
                Formatter.Write(">");
            }
        }

        private void GenerateReference(TypescriptTypeBase referencedType)
        {
            if (referencedType is ArrayType)
            {
                GenerateReference((ArrayType)referencedType);
            }
            else if (referencedType is PrimitiveType)
            {
                GenerateReference((PrimitiveType)referencedType);
            }
            else if (referencedType is EnumType)
            {
                GenerateReference((EnumType)referencedType);
            }
            else if (referencedType is DeclarationBase)
            {
                GenerateReference((DeclarationBase)referencedType);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }            
        }

        private void GenerateReference(DeclarationBase type)
        {
            //TODO: module
            Formatter.Write(type.Name);
        }
        private void GenerateReference(EnumType type)
        {
            //TODO: module
            Formatter.Write(type.Name);
        }
        private void GenerateReference(PrimitiveType type)
        {
            //TODO: module
            Formatter.Write(type.Name);
        }

        private void GenerateReference(ArrayType t)
        {
            //TODO: module
            Generate(t.ElementType);
            Formatter.Write("[]");
        }

        private void Generate(GenericParameter obj)
        {
            Formatter.Write(obj.Name);
        }

        public void Generate(RawStatements raw)
        {
            foreach (var item in raw.Statements)
            {
                if (item is RawStatement)
                {
                    Formatter.Write(((RawStatement)item).Content);
                }
                else if (item is RawStatementTypeReference)
                {
                    Generate(((RawStatementTypeReference)item).TypeReference);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
