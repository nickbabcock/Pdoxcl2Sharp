using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Pdoxcl2Sharp.Test
{
    public class SimpleString
    {
        public string Unit { get; set; }
    }

    public class SimplePOD
    {
        public DateTime Date { get; set; }
        public int Int { get; set; }
        public uint Uint { get; set; }
        public string @String { get; set; }
        public double Double { get; set; }
    }

    public class Lists
    {
        public IEnumerable<int> IntList { get; set; }
        public IEnumerable<string> StringList { get; set; }
        public IEnumerable<DateTime> DateList { get; set; }
        public IEnumerable<double> DoubleList { get; set; }
    }

    public class Collections
    {
        public ICollection<string> Collection { get; set; }
        public string[] Array { get; set; }
        public List<string> List { get; set; }
        public LinkedList<string> LinkedList { get; set; }
    }

    public class SimpleAlias
    {
        [ParadoxAlias("unit")]
        public string Unit { get; set; }
    }

    [TestFixture]
    public class DeserializerTests
    {
        [Test]
        public void DeserializeSimpleString()
        {
            var data = "Unit=infantry".ToStream();
            var actual = ParadoxParser.Deserialize<SimpleString>(data);
            Assert.AreEqual("infantry", actual.Unit);
        }

        [Test]
        public void DeserializeQuotedString()
        {
            var data = "Unit=\"cavalry\"".ToStream();
            var actual = ParadoxParser.Deserialize<SimpleString>(data);
            Assert.AreEqual("cavalry", actual.Unit);
        }

        [Test]
        public void DeserializeSimplePOD()
        {
            var data = "Date=\"1911.1.1\" Int=-1 Uint=1 String=A Double=3.000".ToStream();
            var actual = ParadoxParser.Deserialize<SimplePOD>(data);
            Assert.AreEqual(new DateTime(1911, 1, 1), actual.Date);
            Assert.AreEqual(-1, actual.Int);
            Assert.AreEqual(1, actual.Uint);
            Assert.AreEqual("A", actual.String);
            Assert.AreEqual(3.000, actual.Double);
        }

        [Test]
        public void DeserializeLists()
        {
            var data = @"IntList={1 2}
StringList={A B C}
DateList={""1993.2.19""}
DoubleList={3.000}".ToStream();

            var actual = ParadoxParser.Deserialize<Lists>(data);
            CollectionAssert.AreEqual(new[] { 1, 2 }, actual.IntList);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, actual.StringList);
            CollectionAssert.AreEqual(new[] { new DateTime(1993, 2, 19) }, actual.DateList);
            CollectionAssert.AreEqual(new[] { 3.000 }, actual.DoubleList);
        }

        [Test]
        public void DeserializeWithAlias()
        {
            var data = "unit=infantry".ToStream();
            var actual = ParadoxParser.Deserialize<SimpleAlias>(data);
            Assert.AreEqual("infantry", actual.Unit);
        }

        [Test]
        public void DeserializeCollections()
        {
            var data = @"Collection={ A B C}
Array={D E F }
List = {H I J}
LinkedList = { K 11 12 }".ToStream();

            var actual = ParadoxParser.Deserialize<Collections>(data);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, actual.Collection);
            CollectionAssert.AreEqual(new[] { "D", "E", "F" }, actual.Array);
            CollectionAssert.AreEqual(new[] { "H", "I", "J" }, actual.List);
            CollectionAssert.AreEqual(new[] { "K", "11", "12" }, actual.LinkedList);
        }
    }
}
