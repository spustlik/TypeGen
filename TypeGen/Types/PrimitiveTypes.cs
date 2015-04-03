using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeGen
{
    public abstract class PrimitiveType : TypescriptTypeBase
    {
        public abstract string Name { get; }
        public bool IsNative { get; protected set; }
        protected PrimitiveType()
        {
            IsNative = true;
        }
        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == GetType();
        }
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
        public static readonly AnyType Any = new AnyType();
        public static readonly NumberType Number = new NumberType();
        public static readonly StringType String = new StringType();
        public static readonly BoolType Boolean = new BoolType();
        public static readonly VoidType Void = new VoidType();
        public static readonly DateTimeType Date = new DateTimeType();
        
    }

    public sealed class AnyType : PrimitiveType
    {
        public override string Name
        {
            get { return "any"; }
        }
    }
    public sealed class VoidType : PrimitiveType
    {
        public override string Name
        {
            get { return "void"; }
        }
    }

    public sealed class BoolType : PrimitiveType
    {
        public override string Name
        {
            get { return "boolean"; }
        }
    }

    public sealed class StringType : PrimitiveType
    {
        public override string Name
        {
            get { return "string"; }
        }
    }

    public sealed class NumberType : PrimitiveType
    {        
        public override string Name
        {
            get { return "number"; }
        }
    }

    public sealed class GuidType : PrimitiveType
    {
        public GuidType()
        {
            IsNative = false;
        }
        public override string Name
        {
            get { return "string"; }
        }
    }

    public sealed class DateTimeType : PrimitiveType
    {
        public DateTimeType()
        {
            IsNative = false;
        }
        public override string Name
        {
            get { return "Date"; }
        }
    }
}
