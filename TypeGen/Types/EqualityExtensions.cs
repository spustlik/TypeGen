using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public static class EqualityExtensions
    {
        //modules are not compared
        //enums, classes and interfaces are compared to reference
        // it is considerable to compare shapes, exact order of members, types only, etc.
        // shapes comparing is not possible in TypeDom, because of raw property initializators and raw members

        public static bool IsSame(this EnumMember m1, EnumMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            return m1.Name == m2.Name
                && m1.Comment == m2.Comment
                && m1.Value.IsSame(m2.Value);
        }

        public static bool IsSame(this DeclarationMember m1, DeclarationMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            if (m1.GetType() != m2.GetType())
                return false;
            if (m1 is PropertyMember)
                return IsSame((PropertyMember)m1, (PropertyMember)m2);
            if (m1 is FunctionMemberBase)
                return IsSame((FunctionMemberBase)m1, (FunctionMemberBase)m2);
            if (m1 is RawDeclarationMember)
                return IsSame((RawDeclarationMember)m1, (RawDeclarationMember)m2);
            throw new NotImplementedException();
        }

        public static bool IsSame(this RawDeclarationMember m1, RawDeclarationMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            return m1.Raw.IsSame(m2.Raw)
                && m1.Comment == m2.Comment;
        }

        public static bool IsSame(this FunctionMemberBase m1, FunctionMemberBase m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            if (m1.GetType() != m2.GetType())
                return false;
            if (m1 is FunctionDeclarationMember)
                return IsSame((FunctionDeclarationMember)m1, (FunctionDeclarationMember)m2);
            if (m1 is FunctionMember)
                return IsSame((FunctionMember)m1, (FunctionMember)m2);
            throw new NotImplementedException();
        }

        public static bool IsSame(this FunctionMember m1, FunctionMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            if (!m1.Body.IsSame(m2.Body))
                return false;
            return IsSameBase(m1, m2);
        }

        public static bool IsSame(this FunctionDeclarationMember m1, FunctionDeclarationMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            return IsSameBase(m1, m2);
        }

        private static bool IsSameBase(FunctionMemberBase m1, FunctionMemberBase m2)
        {
            if (!Enumerable.SequenceEqual(m1.GenericParameters, m2.GenericParameters, new GenericParameterEqualityComparer()))
                return false;
            if (HasSameParameters(m1, m2))
                return false;
            return m1.Accessibility == m2.Accessibility
                && m1.Comment == m2.Comment
                && m1.ResultType.IsSame(m2.ResultType);
        }

        public static bool HasSameParameters(this FunctionMemberBase m1, FunctionMemberBase m2)
        {
            return !Enumerable.SequenceEqual(m1.Parameters, m2.Parameters, new FunctionParameterEqualityComparer());
        }

        public static bool IsSame(this PropertyMember m1, PropertyMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            return m1.Name == m2.Name 
                && IsSamePropertyDeclaration(m1, m2);
        }

        /// <summary>
        /// returns true, if properties are same in declaration part, i.e. only names can differ
        /// </summary>
        public static bool IsSamePropertyDeclaration(this PropertyMember m1, PropertyMember m2)
        {
            if (m1 == null || m2 == null)
                return m1 == m2;
            return m1.MemberType.IsSame(m2.MemberType)
                            && m1.IsOptional == m2.IsOptional
                            && m1.Initialization.IsSame(m2.Initialization);
        }

        public static bool IsSame(this TypescriptTypeReference t1, TypescriptTypeReference t2)
        {
            if (t1 == null || t2 == null)
                return t1 == t2;
            if (!IsSame(t1.GenericParameters, t2.GenericParameters))
                return false;
            if (t1.ReferencedType != null)
                return IsSame(t1.ReferencedType, t2.ReferencedType);
            if (t1.TypeName != t2.TypeName)
                return false;
            return IsSame(t1.Raw, t2.Raw);
        }

        public static bool IsSame(IEnumerable<TypescriptTypeReference> t1, IEnumerable<TypescriptTypeReference> t2)
        {
            return Enumerable.SequenceEqual(t1, t2, new TypescriptTypeReferenceEqualityComparer());
        }

        public static bool IsSame(this TypescriptTypeBase t1, TypescriptTypeBase t2)
        {
            if (t1 == null || t2 == null)
                return t1 == t2;
            if (t1.GetType() != t2.GetType())
                return false;
            if (t1 is PrimitiveType)
                return IsSame((PrimitiveType)t1, (PrimitiveType)t2);
            if (t1 is ArrayType)
                return IsSame((ArrayType)t1, (ArrayType)t2);
            return t1 == t2;
        }

        public static bool IsSame(this ArrayType t1, ArrayType t2)
        {
            if (t1 == null || t2 == null)
                return t1 == t2;
            return t1.ElementType.IsSame(t2.ElementType);
        }

        public static bool IsSame(this PrimitiveType t1, PrimitiveType t2)
        {
            if (t1 == null || t2 == null)
                return t1 == t2;
            if (t1 == t2)
                return true;
            return t1.Name == t2.Name;
        }

        public static bool IsSame(this RawStatements r1, RawStatements r2)
        {
            if (r1 == null || r2 == null)
                return r1==r2;
            return Enumerable.SequenceEqual(r1.Statements, r2.Statements, new RawStatementEqualityComparer());
        }

        public static bool IsSame(this RawStatementBase r1, RawStatementBase r2)
        {
            if (r1 == null || r2 == null)
                return r1 == r2;
            if (r1.GetType() != r2.GetType())
                return false;
            if (r1 is RawStatementTypeReference)
            {
                return IsSame((RawStatementTypeReference)r1, (RawStatementTypeReference)r2);
            }
            else if (r1 is RawStatement)
            {
                return IsSame((RawStatement)r1, (RawStatement)r2);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static bool IsSame(this RawStatement r1, RawStatement r2)
        {
            if (r1 == null || r2 == null)
                return r1 == r2;
            return r1.Content == r2.Content;
        }

        public static bool IsSame(this RawStatementTypeReference r1, RawStatementTypeReference r2)
        {
            if (r1 == null || r2 == null)
                return r1 == r2;
            return r1.TypeReference.IsSame(r2.TypeReference);
        }

        public static bool IsSame(this GenericParameter p1, GenericParameter p2)
        {
            if (p1 == null || p2 == null)
                return p1 == p2;
            return p1.Comment == p2.Comment
                && p1.Constraint.IsSame(p2.Constraint)
                && p1.Name == p2.Name;
        }

        public static bool IsSame(this FunctionParameter p1, FunctionParameter p2)
        {
            if (p1 == null || p2 == null)
                return p1 == p2;
            return
                p1.Name == p2.Name
                && p1.ParameterType.IsSame(p2.ParameterType)
                && p1.Comment == p2.Comment
                && p1.DefaultValue.IsSame(p2.DefaultValue)
                && p1.IsOptional == p2.IsOptional
                && p1.IsRest == p2.IsRest;                
        }

        private class TypescriptTypeReferenceEqualityComparer : IEqualityComparer<TypescriptTypeReference>
        {
            bool IEqualityComparer<TypescriptTypeReference>.Equals(TypescriptTypeReference x, TypescriptTypeReference y)
            {
                return x.IsSame(y);
            }

            int IEqualityComparer<TypescriptTypeReference>.GetHashCode(TypescriptTypeReference obj)
            {
                return obj.GetHashCode();
            }
        }

        private class RawStatementEqualityComparer : IEqualityComparer<RawStatementBase>
        {
            bool IEqualityComparer<RawStatementBase>.Equals(RawStatementBase x, RawStatementBase y)
            {
                return x.IsSame(y);
            }

            int IEqualityComparer<RawStatementBase>.GetHashCode(RawStatementBase obj)
            {
                return obj.GetHashCode();
            }
        }

        private class GenericParameterEqualityComparer : IEqualityComparer<GenericParameter>
        {
            bool IEqualityComparer<GenericParameter>.Equals(GenericParameter x, GenericParameter y)
            {
                return x.IsSame(y);
            }

            int IEqualityComparer<GenericParameter>.GetHashCode(GenericParameter obj)
            {
                return obj.GetHashCode();
            }
        }

        private class FunctionParameterEqualityComparer : IEqualityComparer<FunctionParameter>
        {
            bool IEqualityComparer<FunctionParameter>.Equals(FunctionParameter x, FunctionParameter y)
            {
                return x.IsSame(y);
            }

            int IEqualityComparer<FunctionParameter>.GetHashCode(FunctionParameter obj)
            {
                return obj.GetHashCode();
            }
        }

    }
}
