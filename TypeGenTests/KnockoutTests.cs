using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class KnockoutTests
    {
        [TestMethod]
        public void TestInterfacesToObservableInterfaces()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    interface ITest1 {
        Prop1: string;
        Prop2: number;
    }
    interface ITest1B extends ITest1 {
        Prop3: boolean;
        Ref: ITest1;
        PropArray: string[];
        SelfArray: ITest1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, true);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    interface IObservableTest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface IObservableTest1B extends IObservableTest1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<IObservableTest1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<IObservableTest1B>;
    }
}".Trim(), o.Output.Trim());

        }

        [TestMethod]
        public void TestClassesToObservableInterfaces()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = true;
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    class Test1 {
        Prop1: string;
        Prop2: number;
    }
    class Test1B extends Test1 {
        Prop3: boolean;
        Ref: Test1;
        PropArray: string[];
        SelfArray: Test1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, true);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    interface IObservableTest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface IObservableTest1B extends IObservableTest1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<IObservableTest1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<IObservableTest1B>;
    }
}
".Trim(), o.Output.Trim());

        }
        [TestMethod]
        public void TestClassesToObservableClasses()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = true;
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    class Test1 {
        Prop1: string;
        Prop2: number;
    }
    class Test1B extends Test1 {
        Prop3: boolean;
        Ref: Test1;
        PropArray: string[];
        SelfArray: Test1B[];
    }
}
".Trim(), o1.Output.Trim());

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, false);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(@"
module Observables {
    class test1B {
        Prop3 = ko.observable<boolean>();
        Ref = ko.observable<test1>();
        PropArray = ko.observableArray<string>();
        SelfArray = ko.observableArray<test1B>();
    }
    class test1 {
        Prop1 = ko.observable<string>();
        Prop2 = ko.observable<number>();
    }
}
".Trim(), o.Output.Trim());

        }

        [TestMethod]
        public void TestInterfacesToObservableClasses()
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = false;
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, false);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.IsNull(Helper.StringCompare(@"
module Observables {
    class test1B implements IObservableTest1 {        
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
        Prop3 = ko.observable<boolean>();
        Ref = ko.observable<test1>();
        PropArray = ko.observableArray<string>();
        SelfArray = ko.observableArray<test1B>();
    }
    interface IObservableTest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    class test1 {
        Prop1 = ko.observable<string>();
        Prop2 = ko.observable<number>();
    }
}
", o.Output));

        }


        private static ReflectionGenerator test3refl(bool sourceClasses)
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = sourceClasses;
            rg.NamingStrategy.InterfacePrefix = "";
            rg.GenerateTypes(new[] { typeof(Test3), typeof(Test3A) });
            return rg;
        }

        private static string test3reflstr(bool sourceClasses)
        {
            var rg = test3refl(sourceClasses);
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            return o1.Output;
        }
        private static string test3(bool sourceClasses, bool destClasses)
        {
            var rg = test3refl(sourceClasses);
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            var o1Output = o1.Output;

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, !destClasses);

            var o = new OutputGenerator();
            o.Generate(observables);
            return o.Output;
        }

        [TestMethod]
        public void TestInheritance2()
        {
            var result = test3reflstr(sourceClasses: true);
            Assert.AreEqual(null, Helper.StringCompare(@"
module GeneratedModule {
    class Test3A implements ITest3A {
        Prop1: number;
    }
    class Test3 extends Test3A implements ITest3C, ITest3B {
        Prop2: string;
        Prop3: string;
    }
    class ITest3A {
        Prop1: number;
    }
    class ITest3C implements ITest3A, ITest3B {
        Prop3: string;
    }
    class ITest3B implements ITest3A {
        Prop2: string;
    }
}
", result));
        }

        [TestMethod]
        public void TestInheritanceIntf()
        {
            string result = "";
            result = test3(sourceClasses: true, destClasses: false);
            Assert.AreEqual(@"
module Observables {
    interface IObservableITest3A {
        Prop1: KnockoutObservable<number>;
    }
    interface IObservableTest3A extends IObservableITest3A {
        Prop1: KnockoutObservable<number>;
    }
    interface IObservableITest3B extends IObservableITest3A {
        Prop2: KnockoutObservable<string>;
    }
    interface IObservableITest3C extends IObservableITest3A, IObservableITest3B {
        Prop3: KnockoutObservable<string>;
    }
    interface IObservableTest3 extends IObservableTest3A, IObservableITest3C, IObservableITest3B {
        Prop2: KnockoutObservable<string>;
        Prop3: KnockoutObservable<string>;
    }
}
".Trim(), result.Trim());

        }

        [TestMethod]
        public void TestInheritanceClassesFromIntf()
        {
            string result = "";
            result = test3(sourceClasses: false, destClasses: true);
            Assert.AreEqual(null, Helper.StringCompare(@"
module Observables {
    class test3 implements IObservableTest3A, IObservableTest3C, IObservableTest3B {     
        // implementation of IObservableTest3A   
        Prop1 = ko.observable<number>();
        // implementation of IObservableTest3B
        Prop2 = ko.observable<string>();
        // implementation of IObservableTest3C
        Prop3 = ko.observable<string>();
    }
    interface IObservableTest3A {
        Prop1: KnockoutObservable<number>;
    }
    interface IObservableTest3B extends IObservableTest3A {
        Prop2: KnockoutObservable<string>;
    }
    interface IObservableTest3C extends IObservableTest3A, IObservableTest3B {
        Prop3: KnockoutObservable<string>;
    }
    class test3A implements IObservableTest3A {
        Prop1 = ko.observable<number>();
    }
    class test3A {
        Prop1 = ko.observable<number>();
    }
    class test3C implements IObservableTest3A, IObservableTest3B {
        // implementation of IObservableTest3A
        Prop1: KnockoutObservable<number>;
        // implementation of IObservableTest3B
        Prop2: KnockoutObservable<string>;
        Prop3 = ko.observable<string>();
    }
    class test3B implements IObservableTest3A {
        // implementation of IObservableTest3A
        Prop1: KnockoutObservable<number>;
        Prop2 = ko.observable<string>();
    }

////ERROR:
    interface IObservableTest3A extends IObservableTest3A {
        Prop1: KnockoutObservable<number>;
    }

}
", result));
        }

        [TestMethod]
        public void TestInheritanceClasses()
        {
            string result = "";
            result = test3(sourceClasses: false, destClasses: true);
            Assert.AreEqual(null, Helper.StringCompare(@"
module Observables {
    class test3 implements IObservableITest3C, IObservableITest3B {
        Prop1 = ko.observable<number>();
        Prop2 = ko.observable<string>();
        Prop3 = ko.observable<string>();
    }
    interface IObservableITest3A {
        Prop1: KnockoutObservable<number>;
    }
    interface IObservableITest3B extends IObservableITest3A {
        Prop2: KnockoutObservable<string>;
    }
    interface IObservableITest3C extends IObservableITest3A, IObservableITest3B {
        Prop3: KnockoutObservable<string>;
    }
    class test3A implements IObservableITest3A {
        Prop1 = ko.observable<number>();
    }
    class observableITest3C implements IObservableITest3A, IObservableITest3B {
        Prop1 = ko.observable<number>();
        Prop2: KnockoutObservable<string>;
        Prop3 = ko.observable<string>();
    }
    class observableITest3B implements IObservableITest3A {
        Prop1 = ko.observable<number>();
        Prop2 = ko.observable<string>();
    }
}
", result));
            // tridy typu "iTestxxx" nejsou nutne, pokud existuji jiz testxxx (viz iTest3c)
            // implementacni tridy muzou vyuzit dedicnost, bohuzel se ztraci v reflection generatoru do intf
        }    
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
    }

    public class Test3A : ITest3A
    {
        public int Prop1 { get; set; }
    }
    public class Test3 : Test3A, ITest3C
    {
        public string Prop2 { get; set; }
        public string Prop3 { get; set; }
    }

}
