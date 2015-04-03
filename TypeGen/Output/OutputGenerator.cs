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

        public void Generate(Module m)
        {
            Formatter.Write("module ");
            Formatter.Write(m.Name);
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            Formatter.WriteSeparated("\n", m.Members, Generate);
            Formatter.PopIndent();
            Formatter.WriteEndOfLine();
            Formatter.Write("}");
        }

        private void Generate(ModuleElement element)
        {
            if (element.IsExporting)
            {
                Formatter.Write("export ");
            }            
            if (element is DeclarationModuleElement)
            {
                Generate((DeclarationModuleElement)element);
            }
            else if (element is RawModuleElement)
            {
                Generate((RawModuleElement)element);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(DeclarationModuleElement element)
        {
            if (element.Declaration != null)
            {
                Generate(element.Declaration);
            }
            else if (element.EnumDeclaration != null)
            {
                Generate(element.EnumDeclaration);
            }
            else if (element.InnerModule != null)
            {
                Generate(element.InnerModule);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(RawModuleElement element)
        {
            Generate(element.Raw);
        }

        public void Generate(EnumType type)
        {
            Formatter.Write("enum ");
            Formatter.Write(type.Name);
            Formatter.Write(" {");
            Formatter.WriteLine();
            Formatter.PushIndent();
            Formatter.WriteSeparated(",\n", type.Members, Generate);
            Formatter.WriteEndOfLine();
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
                Formatter.WriteEndOfLine();
            }
            Formatter.PopIndent();
            Formatter.WriteEndOfLine();
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
                Formatter.WriteEndOfLine();
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
            else if (m is FunctionMemberBase)
            {
                Generate((FunctionMemberBase)m);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(FunctionMemberBase fn)
        {
            Generate(fn.Accessibility);
            Formatter.Write("function ");
            Formatter.Write(fn.Name);
            if (fn.IsGeneric)
            {
                Formatter.Write("<");
                Formatter.WriteSeparated(", ", fn.GenericParameters, Generate);
                Formatter.Write(">");
            }
            Formatter.Write("(");
            Formatter.WriteSeparated(", ", fn.Parameters, Generate);
            Formatter.Write(")");
            if (fn.ResultType != null)
            {
                Formatter.Write(": ");
                Generate(fn.ResultType);
            }            
            if (fn is FunctionDeclarationMember)
            {
                Formatter.Write(";");
            }
            else if (fn is FunctionMember)
            {
                Formatter.Write("{");
                Formatter.WriteLine();
                Formatter.PushIndent();
                var fnm = (FunctionMember)fn;
                if (fnm.Body != null)
                {
                    Generate(fnm.Body);
                }
                Formatter.PopIndent();
                Formatter.WriteEndOfLine();
                Formatter.Write("}");
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Generate(FunctionParameter par)
        {
            if (par.IsRest)
            {
                Formatter.Write("...");
            }
            Formatter.Write(par.Name);
            if (par.IsOptional)
            {
                Formatter.Write("?");
            }
            if (par.ParameterType != null)
            {
                Formatter.Write(": ");
                Generate(par.ParameterType);
            }
            if (par.DefaultValue != null)
            {
                Formatter.Write(" = ");
                Generate(par.DefaultValue);
            }
        }

        private void Generate(PropertyMember m)
        {
            Generate(m.Accessibility);
            Formatter.Write(m.Name);
            if (m.IsOptional)
                Formatter.Write("?");
            Formatter.Write(": ");
            Generate(m.MemberType);
            Formatter.Write(";");
        }

        private void Generate(AccessibilityEnum? accessibility)
        {
            if (accessibility != null)
            {
                Formatter.Write(accessibility.Value.ToStr());
                Formatter.Write(" ");
            }
        }

        private void Generate(TypescriptTypeReference obj)
        {
            if (!String.IsNullOrEmpty(obj.TypeName))
            {
                Formatter.Write(obj.TypeName);
            }
            else if (obj.Raw != null)
            {
                Generate(obj.Raw);
            }
            else
            {
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
            if (obj.Constraint != null)
            {
                Formatter.Write(" extends ");
                Generate(obj.Constraint);
            }
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
