using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class KnockoutReflectionTests
    {
        [TestMethod]
        public void TestKoReflection()
        {
            var kogen = new KnockoutReflectionGenerator();
            kogen.GenerateFromType(typeof(Test1B));
            var o = new OutputGenerator();
            o.GenerateModuleContent(kogen.Module, null);
            Assert.AreEqual(null, Helper.StringCompare(@"
class test1 {
    Prop1 = ko.observable<string>();
    Prop2 = ko.observable<number>();
}
class test1B extends test1 {
    Prop3 = ko.observable<boolean>();
    Ref = ko.observable<test1>();
    PropArray = ko.observableArray<string>();
    SelfArray = ko.observableArray<test1B>();
}", o.Output));
        }

        [TestMethod]
        public void TestKoInheritance()
        {
            var kogen = new KnockoutReflectionGenerator();
            kogen.GenerateFromType(typeof(Test3));            
            var o = new OutputGenerator();
            o.GenerateModuleContent(kogen.Module, null);
            Assert.AreEqual(null, Helper.StringCompare(@"
class test3A implements IObservableITest3A {
    Prop1 = ko.observable<number>();
}
class test3 extends test3A implements IObservableITest3B, IObservableITest3C {
    Prop2 = ko.observable<string>();
    Prop3 = ko.observable<string>();
    Prop4 = ko.observable<IObservableITest3A>();
    PropOwn = ko.observable<test3A>();
}
interface IObservableITest3A {
    Prop1: KnockoutObservable<number>;
}
interface IObservableITest3B extends IObservableITest3A {
    Prop2: KnockoutObservable<string>;
}
interface IObservableITest3C extends IObservableITest3A, IObservableITest3B {
    Prop3: KnockoutObservable<string>;
    Prop4: KnockoutObservable<IObservableITest3A>;
}
", o.Output));
        }


        class Test1
        {
            public string Prop1 { get; set; }
            public int Prop2 { get; set; }
        }

        class Test1B : Test1
        {
            public bool Prop3 { get; set; }
            public Test1 Ref { get; set; }

            public string[] PropArray { get; set; }
            public Test1B[] SelfArray { get; set; }
        }

        public interface ITest3A
        {
            int Prop1 { get; set; }
        }

        public interface ITest3B : ITest3A
        {
            string Prop2 { get; set; }
        }

        public interface ITest3C : ITest3A, ITest3B
        {
            string Prop3 { get; set; }
            ITest3A Prop4 { get; set; }
        }

        public class Test3A : ITest3A
        {
            public int Prop1 { get; set; }
        }
        public class Test3 : Test3A, ITest3C
        {
            public string Prop2 { get; set; }
            public string Prop3 { get; set; }
            public ITest3A Prop4 { get; set; }
            public Test3A PropOwn { get; set; }
        }

    }
}
