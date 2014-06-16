using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    public class DeserializeTest
    {
        [TestCase("01", (byte)1)]
        [TestCase(" { 1 } ", (ushort)1)]
        [TestCase(" 1 ", (uint)1)]
        [TestCase("\r\n{{-100}}\r\n", (short)-100)]
        [TestCase("-1", (int)-1)]
        [TestCase("# -1 \r\n-2", (sbyte)-2)]
        [TestCase("213982108", (ulong)213982108)]
        [TestCase("-213982108", (long)-213982108)]
        [TestCase("yes", true)]
        [TestCase("no", false)]
        [TestCase("you=me", "you")]
        public void DesrializeTest<T>(string data, T expected)
        {
            T actual = ParadoxParser.Deserialize<T>(data.ToStream());
            Assert.AreEqual(expected, actual);
        }

        [TestCase("1942.6.3.2.30.59", "1942/6/3 02:30:59")]
        [TestCase("1942.6.3.2.30", "1942/6/3 02:30")]
        [TestCase("1942.6.3.2", "1942/6/3 02:00")]
        [TestCase("1942.6.3", "1942/6/3")]
        [TestCase("1942.6", "1942/6/1")]
        [TestCase("1942", "1942/1/1")]
        public void DeserializeDates(string data, DateTime expected)
        {
            DateTime actual = ParadoxParser.Deserialize<DateTime>(data.ToStream());
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeNullable()
        {
            int? actual = ParadoxParser.Deserialize<int?>("1".ToStream());
            Assert.AreEqual(1, actual);
        }

        [Test]
        public void DeserializeStringArray()
        {
            string data = "{me {you them} us} #hehe";
            string[] expected = { "me", "you", "them", "us" };
            string[] actual = ParadoxParser.Deserialize<string[]>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeIntArray()
        {
            string data = "{1, 2 -1, 0 3}";
            int[] expected = { 1, 2, -1, 0, 3 };
            int[] actual = ParadoxParser.Deserialize<int[]>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeStringCollection()
        {
            string data = "{me {you them} us} #hehe";
            ICollection<string> expected = new[] { "me", "you", "them", "us" };
            ICollection<string> actual = ParadoxParser.Deserialize<ICollection<string>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeStringIEnumerable()
        {
            string data = "{me {you them} us} #hehe";
            IEnumerable<string> expected = new[] { "me", "you", "them", "us" };
            var actual = ParadoxParser.Deserialize<IEnumerable<string>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeStringList()
        {
            string data = "{me {you them} us} #hehe";
            var expected = new List<string> { "me", "you", "them", "us" };
            var actual = ParadoxParser.Deserialize<List<string>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeDictionary()
        {
            string data = "{me=1.000 you=2.500} #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<Dictionary<string, double>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeIDictionary()
        {
            string data = "{me=1.000 you=2.500} #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<IDictionary<string, double>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DeserializeWithoutInitialCurly()
        {
            string data = "me=1.000 you=2.500 #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<IDictionary<string, double>>(data.ToStream());
            CollectionAssert.AreEqual(expected, actual);
        }

        public class Bar
        {
            public int id { get; set; }
        }

        public class Foo
        {
            public string value { get; set; }
            public Bar bar { get; set; }
        }

        [Test]
        public void DeserializeObjects()
        {
            string data = "{value=\"Hey\" bar={id=4}}";
            var actual = ParadoxParser.Deserialize<Foo>(data.ToStream());
            Assert.AreEqual("Hey", actual.value);
            Assert.AreEqual(4, actual.bar.id);
        }

        public class Foo2 : IParadoxRead
        {
            public int value { get; set; }
            public void TokenCallback(ParadoxParser parser, string token)
            {
                value = parser.ReadInt32() + 1;
            }
        }

        [Test]
        public void DeserializeIParadoxReads()
        {
            string data = "{value=4 value=5}";
            var actual = ParadoxParser.Deserialize<Foo2>(data.ToStream());
            Assert.AreEqual(6, actual.value);
        }
    }
}
