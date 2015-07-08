using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeGen.Generators;

namespace TypeGenTests
{
    [TestClass]
    public class NamingTests
    {
        [TestMethod]
        public void TestCamelCase()
        {
            {
                var s = NamingHelper.CamelCaseFromString(" hello world, this is some long string ");
                Assert.AreEqual("HelloWorldThisIsSomeLongString", s);
            }
            {
                var s = NamingHelper.CamelCaseFromString("HelloWorld");
                Assert.AreEqual("HelloWorld", s);
            }
            {
                var s = NamingHelper.CamelCaseFromString("hi SQL world");
                Assert.AreEqual("HiSQLWorld", s);
            }
        }
    }
}
