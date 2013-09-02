using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    public class NamingConventionTests
    {
        [Test]
        public void UnderscoreSingleTest()
        {
            var convention = new UnderscoreNamingConvention();
            var actual = convention.Apply("Unit");
            Assert.AreEqual("unit", actual);
        }

        [Test]
        public void UnderscoreMultipleTest()
        {
            var convention = new UnderscoreNamingConvention();
            var actual = convention.Apply("MilitaryConstruction");
            Assert.AreEqual("military_construction", actual);
        }
    }
}
