using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;
using TypeGen;
using Newtonsoft.Json;

namespace TypeGenTests
{
    [TestClass]
    public class ReflectionTests2
    {
        public interface ITest3A
        {
            int Prop1 { get; set; }
        }

        public interface ITest3B : ITest3A
        {
            string Prop2 { get; set; }
            ITest3A Prop2b { get; set; }
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
            public ITest3A Prop2b { get; set; }
            public string Prop3 { get; set; }
            [JsonIgnore]
            public string IgnoredProperty { get; set; }
        }

        private string test(bool classes)
        {
            var rg = new ReflectionGenerator();
            rg.GenerationStrategy.CommentSource = false;
            rg.GenerationStrategy.GenerateClasses = classes;
            rg.GenerateFromType(typeof(Test3));
            var module = rg.Module;
            module = Optimizer.RemoveEmptyDeclarations(module);
            var o = new OutputGenerator();
            o.Generate(module);
            return o.Output;
        }

        [TestMethod]
        public void TestReflectionInheritanceOptimalization1()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateInterface(typeof(ITest3B));

            var g = new OutputGenerator();
            g.Generate(rg.GenerationStrategy.TargetModule);

            Assert.AreEqual(null, Helper.StringCompare(
@"module GeneratedModule {
    interface ITest3A {
        Prop1: number;
    }
    interface ITest3B extends ITest3A {
        Prop2: string;
        Prop2b: ITest3A;
    }
}", g.Output));
        }

        [TestMethod]
        public void TestReflectionInheritanceOptimalization2()
        {
            var rg = new ReflectionGenerator();
            rg.GenerateInterface(typeof(Test3A));

            var module = rg.GenerationStrategy.TargetModule;
            module = Optimizer.RemoveEmptyDeclarations(module);
            var g = new OutputGenerator();
            g.Generate(module);

            Assert.AreEqual(null, Helper.StringCompare(
@"module GeneratedModule {
    interface ITest3A {
        Prop1: number;
    }
}", g.Output));
        }


        [TestMethod]
        public void TestReflectionInheritanceOptimalization3()
        {
            //generator is instructed to generate INTERFACES from given types
            var result = test(classes: false);
            Assert.AreEqual(null, Helper.StringCompare(@"
module GeneratedModule {
    interface ITest3 extends ITest3A, ITest3B, ITest3C {
    }
    interface ITest3A {
        Prop1: number;
    }
    interface ITest3C extends ITest3A, ITest3B {
        Prop3: string;
    }
    interface ITest3B extends ITest3A {
        Prop2: string;
        Prop2b: ITest3A;
    }
}
", result));
        }

    }


}

