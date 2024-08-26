using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen.Visitors
{
    public abstract class VisitorBase
    {
        protected virtual void NonVisitable(object element, string message)
        {            
            throw new InvalidOperationException(String.Format("Cannot visit " + message, element ?? "<null>"));
        }

        public virtual void Visit(TypescriptModule module)
        {
            VisitModule(module);
        }

        public virtual void VisitModule(TypescriptModule module)
        {
            foreach (var moduleMember in module.Members.ToArray())
            {
                Visit(moduleMember);
            }
        }

        public virtual void Visit(ModuleElement element)
        {
            if (element is DeclarationModuleElement decl)
            {
                Visit(decl);
            }
            else if (element is RawModuleElement raw)
            {
                VisitRawModuleElement(raw);
            }
            else
            {
                NonVisitable(element, "module element {0}");
            }
        }

        public virtual void Visit(DeclarationModuleElement element)
        {
            if (element.Declaration != null)
            {
                Visit(element.Declaration);
            }
            else if (element.EnumDeclaration != null)
            {
                VisitEnumType(element.EnumDeclaration);
            }
            else if (element.InnerModule != null)
            {
                Visit(element.InnerModule);
            }
            else if (element.TypeDef != null)
            {
                Visit(element.TypeDef);
            }
            else
            {
                NonVisitable(element, "declaration module element {0}");
            }
        }

        public virtual void VisitRawModuleElement(RawModuleElement element)
        {
            VisitRaw(element.Raw);
        }

        public virtual void VisitEnumType(EnumType type)
        {
            foreach (var member in type.Members.ToArray())
            {
                VisitEnumMember(member);
            }
        }

        public virtual void VisitEnumMember(EnumMember m)
        {
            if (m.Value != null)
            {                
                VisitRaw(m.Value);
            }
        }

        public virtual void Visit(TypeDefType typeDef)
        {

        }
        public virtual void Visit(DeclarationBase decl)
        {
            if (decl is ClassType cls)
            {
                VisitClassType(cls);
            }
            else if (decl is InterfaceType intf)
            {
                VisitInterfaceType(intf);
            }
            else
            {
                NonVisitable(decl, "declaration {0}");
            }
        }

        public virtual void VisitGenericParameters(DeclarationBase decl)
        {
            if (decl.IsGeneric)
            {
                foreach (var item in decl.GenericParameters.ToArray())
                {
                    VisitGenericParameter(item);
                }
            }
        }

        public virtual void VisitMembers(DeclarationBase decl)
        {
            foreach (var m in decl.Members.ToArray())
            {
                Visit(m);
            }
        }

        public virtual void VisitClassType(ClassType cls)
        {
            VisitGenericParameters(cls);
            if (cls.Extends != null)
            {
                VisitTypeReference(cls.Extends);
            }
            if (cls.IsImplementing)
            {
                foreach (var item in cls.Implementations.ToArray())
                {
                    VisitTypeReference(item);
                }
            }
            VisitMembers(cls);
        }

        public virtual void VisitInterfaceType(InterfaceType intf)
        {
            VisitGenericParameters(intf);
            if (intf.IsExtending)
            {
                foreach (var item in intf.ExtendsTypes.ToArray())
                {
                    VisitTypeReference(item);
                }
            }
            VisitMembers(intf);
        }

        public virtual void Visit(DeclarationMember m)
        {
            if (m is PropertyMember prop)
            {
                VisitPropertyMember(prop);
            }
            else if (m is FunctionMemberBase fn)
            {
                Visit(fn);
            }
            else if (m is RawDeclarationMember raw)
            {
                VisitRawDeclarationMember(raw);
            }
            else
            {
                NonVisitable(m, "declaration member {0}");
            }
        }

        public virtual void VisitRawDeclarationMember(RawDeclarationMember m)
        {
            if (m.Raw != null)
            {
                VisitRaw(m.Raw);
            }
        }

        public virtual void Visit(FunctionMemberBase fn)
        {
            if (fn is FunctionDeclarationMember fndec)
            {
                VisitFunctionDeclarationMember(fndec);
            }
            else if (fn is FunctionMember fun)
            {
                VisitFunctionMember(fun);
            }
            else
            {
                NonVisitable(fn, "function member {0}");
            }
        }

        public virtual void VisitFunctionMemberBase(FunctionMemberBase fn)
        {
            VisitAccessibility(fn.Accessibility);
            if (fn.IsGeneric)
            {
                foreach (var item in fn.GenericParameters.ToArray())
                {
                    VisitGenericParameter(item);
                }
            }
            foreach (var item in fn.Parameters.ToArray())
            {
                VisitFunctionParameter(item);
            }

            if (fn.ResultType != null)
            {
                VisitTypeReference(fn.ResultType);
            }
        }


        public virtual void VisitFunctionDeclarationMember(FunctionDeclarationMember fn)
        {
            VisitFunctionMemberBase(fn);
        }

        public virtual void VisitFunctionMember(FunctionMember fn)
        {
            VisitFunctionMemberBase(fn);
            if (fn.Body != null)
            {
                VisitRaw(fn.Body);
            }
        }

        public virtual void VisitFunctionParameter(FunctionParameter par)
        {
            if (par.ParameterType != null)
            {
                VisitTypeReference(par.ParameterType);
            }
            if (par.DefaultValue != null)
            {
                VisitRaw(par.DefaultValue);
            }
        }

        public virtual void VisitPropertyMember(PropertyMember m)
        {
            VisitAccessibility(m.Accessibility);
            if (m.MemberType != null)
            {
                VisitTypeReference(m.MemberType);
            }
            if (m.Initialization != null)
            {
                VisitRaw(m.Initialization);
            }
        }

        public virtual void VisitAccessibility(AccessibilityEnum? accessibility)
        {
        }

        public virtual void VisitTypeReference(TypescriptTypeReference obj)
        {
            if (!String.IsNullOrEmpty(obj.TypeName))
            {
                VisitTypeReferenceNamed(obj, obj.TypeName);
            }
            else if (obj.Raw != null)
            {
                VisitTypeReferenceRaw(obj, obj.Raw);
            }
            else
            {
                VisitReference(obj.ReferencedType);
            }
            if (obj.GenericParameters.Count > 0)
            {
                foreach (var item in obj.GenericParameters.ToArray())
                {
                    VisitTypeReference(item);
                }
            }
        }

        public virtual void VisitTypeReferenceRaw(TypescriptTypeReference obj, RawStatements raw)
        {
            VisitRaw(raw);
        }

        public virtual void VisitTypeReferenceNamed(TypescriptTypeReference obj, string name)
        {
        }

        public virtual void VisitReference(TypescriptTypeBase referencedType)
        {
            if (referencedType is ArrayType arr)
            {
                VisitReference(arr);
            }
            else if (referencedType is PrimitiveType pri)
            {
                VisitReference(pri);
            }
            else if (referencedType is EnumType enm)
            {
                VisitReference(enm);
            }
            else if (referencedType is DeclarationBase decl)
            {
                VisitReference(decl);
            }
            else if (referencedType is AnonymousDeclaration an)
            {
                VisitReference(an);
            }
            else
            {
                NonVisitable(referencedType, "referenced type {0}");
            }            
        }

        protected virtual void VisitReference(AnonymousDeclaration an)
        {
            
        }

        public virtual void VisitReference(DeclarationBase type)
        {            
        }

        public virtual void VisitReference(EnumType type)
        {            
        }

        public virtual void VisitReference(PrimitiveType type)
        {
        }

        public virtual void VisitReference(ArrayType type)
        {
            VisitTypeReference(type.ElementType);            
        }

        public virtual void VisitGenericParameter(GenericParameter obj)
        {
            if (obj.Constraint != null)
            {
                VisitTypeReference(obj.Constraint);
            }
        }

        public virtual void VisitRaw(RawStatements raw)
        {
            foreach (var item in raw.Statements.ToArray())
            {
                Visit(raw, item);
            }
        }

        public virtual void Visit(RawStatements raw, RawStatementBase item)
        {
            if (item is RawStatementContent)
            {
                VisitRawStatement(item);
            }
            else if (item is RawStatementTypeReference)
            {
                VisitRawTypeReference(item);
            }
            else
            {
                NonVisitable(raw, "raw statement {0}");
            }
        }

        public virtual void VisitRawTypeReference(RawStatementBase item)
        {
            VisitTypeReference(((RawStatementTypeReference)item).TypeReference);
        }

        public virtual void VisitRawStatement(RawStatementBase item)
        {
        }
    }
}
