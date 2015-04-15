using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;
using TypeGen;

namespace TypeGenTests
{
    [TestClass]
    public class KnockoutTests
    {
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


        [TestMethod]
        public void TestInterfacesToObservableInterfaces()
        {
            var rg = new ReflectionGenerator();
            rg.NamingStrategy.InterfacePrefixForClasses = "i";
            rg.GenerateTypes(new[] { typeof(Test1B), typeof(Test1) });
            var o1 = new OutputGenerator();
            o1.Generate(rg.Module);
            Assert.AreEqual(@"
module GeneratedModule {
    interface iTest1 {
        Prop1: string;
        Prop2: number;
    }
    interface iTest1B extends iTest1 {
        Prop3: boolean;
        Ref: iTest1;
        PropArray: string[];
        SelfArray: iTest1B[];
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
    interface iObservableTest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface iObservableTest1B extends iObservableTest1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<iObservableTest1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<iObservableTest1B>;
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
    interface iObservableTest1 {
        Prop1: KnockoutObservable<string>;
        Prop2: KnockoutObservable<number>;
    }
    interface iObservableTest1B extends iObservableTest1 {
        Prop3: KnockoutObservable<boolean>;
        Ref: KnockoutObservable<iObservableTest1>;
        PropArray: KnockoutObservableArray<string>;
        SelfArray: KnockoutObservableArray<iObservableTest1B>;
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
            Assert.AreEqual(Helper.StringCompare(@"
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
", o1.Output), null);

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(rg.Module, observables, interfaces: false);

            var o = new OutputGenerator();
            o.Generate(observables);
            Assert.AreEqual(null, Helper.StringCompare(@"
module Observables {
    class test1B implements IObservableTest1 {        
        // implementation of IObservableTest1
        Prop1 = ko.observable<string>();
        Prop2 = ko.observable<number>();
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

    }

    [TestClass]
    public class KnockoutTests2
    {
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


        private static Tuple<string,string> runTest(bool sourceClasses, bool destClasses, bool optimize, bool optimizeSource = false)
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.GenerateClasses = sourceClasses;
            rg.NamingStrategy.InterfacePrefixForClasses = "";
            rg.NamingStrategy.InterfacePrefix = "";
            rg.GenerateTypes(new[] { typeof(Test3), typeof(Test3A) });

            var models = rg.Module;
            if (optimizeSource)
            {
                models = Optimizer.RemoveEmptyDeclarations(models);
            }
            var o1 = new OutputGenerator();
            o1.Generate(models);
            var o1Output = o1.Output;

            var ko = new KnockoutGenerator();
            var observables = new TypescriptModule("Observables");
            ko.GenerateObservableModule(models, observables, !destClasses);

            if (optimize)
            {
                observables = Optimizer.RemoveEmptyDeclarations(observables);
            }

            var o2 = new OutputGenerator();
            o2.Generate(observables);
            return Tuple.Create(o1.Output, o2.Output);
        }

        [TestMethod]
        public void _TestSourceClasses()
        {
            var r = runTest(sourceClasses: true, destClasses: false, optimize: false);
            Assert.AreEqual(Helper.StringCompare(@"
module GeneratedModule {
    class Test3A implements ITest3A {
        Prop1: number;
    }
    class Test3 extends Test3A implements ITest3B, ITest3C {
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
", r.Item1), null);
        }
        [TestMethod]
        public void _TestSourceInterfaces()
        {
            var r = runTest(sourceClasses: false, destClasses: false, optimize: false);
            Assert.AreEqual(Helper.StringCompare(@"
module GeneratedModule {
    interface ITest3A {
        Prop1: number;
    }
    interface Test3A extends ITest3A {
    }
    interface ITest3B extends ITest3A {
        Prop2: string;
    }
    interface ITest3C extends ITest3A, ITest3B {
        Prop3: string;
    }
    interface Test3 extends ITest3B, ITest3C, Test3A {
    }
}
", r.Item1), null);
        }


        [TestMethod]
        public void TestInheritanceIntf()
        {
            var r = runTest(sourceClasses: true, destClasses: false, optimize: true);
            Assert.AreEqual(Helper.StringCompare(@"
module Observables {
    interface IObservableITest3A {
        Prop1: KnockoutObservable<number>;
    }
    interface IObservableITest3B extends IObservableITest3A {
        Prop2: KnockoutObservable<string>;
    }
    interface IObservableITest3C extends IObservableITest3A, IObservableITest3B {
        Prop3: KnockoutObservable<string>;
    }
}
", r.Item2), null);
        }

        [TestMethod]
        public void TestInheritanceClassesFromIntf()
        {
            var r = runTest(sourceClasses: false, destClasses: true, optimize: true);
            Assert.AreEqual(Helper.StringCompare(@"
module Observables {
    class test3 implements IObservableTest3A, IObservableTest3B, IObservableTest3C {     
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
        Prop1 = ko.observable<number>();
        // implementation of IObservableTest3B
        Prop2 = ko.observable<string>();
        Prop3 = ko.observable<string>();
    }
    class test3B implements IObservableTest3A {
        // implementation of IObservableTest3A
        Prop1 = ko.observable<number>;
        Prop2 = ko.observable<string>();
    }
}
", r.Item2), null);
        }

        [TestMethod]
        public void TestInheritanceClasses()
        {
            var r = runTest(sourceClasses: false, destClasses: true, optimize: true);
            Assert.AreEqual(Helper.StringCompare(@"
module Observables {
    class test3 implements IObservableITest3B, IObservableITest3C {
        // implementation of IObservableTest3B
        Prop2 = ko.observable<string>();
        // implementation of IObservableTest3C
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
        Prop2 = ko.observable<string>();
        Prop3 = ko.observable<string>();
    }
    class observableITest3B implements IObservableITest3A {
        Prop1 = ko.observable<number>();
        Prop2 = ko.observable<string>();
    }
}
", r.Item2), null);
            // tridy typu "iTestxxx" nejsou nutne, pokud existuji jiz testxxx (viz iTest3c)
            // implementacni tridy muzou vyuzit dedicnost, bohuzel se ztraci v reflection generatoru do intf
        }



    }


}
