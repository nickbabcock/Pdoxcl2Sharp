using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp.Test
{
    public class DeserializeTest
    {
        [Theory]
        [InlineData("01", (byte)1)]
        [InlineData(" { 1 } ", (ushort)1)]
        [InlineData(" 1 ", (uint)1)]
        [InlineData("\r\n{{-100}}\r\n", (short)-100)]
        [InlineData("-1", (int)-1)]
        [InlineData("# -1 \r\n-2", (sbyte)-2)]
        [InlineData("213982108", (ulong)213982108)]
        [InlineData("-213982108", (long)-213982108)]
        [InlineData("yes", true)]
        [InlineData("no", false)]
        [InlineData("you=me", "you")]
        public void DesrializeTest<T>(string data, T expected)
        {
            T actual = ParadoxParser.Deserialize<T>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1942.6.3.2.30.59", "1942/6/3 02:30:59")]
        [InlineData("1942.6.3.2.30", "1942/6/3 02:30")]
        [InlineData("1942.6.3.2", "1942/6/3 02:00")]
        [InlineData("1942.6.3", "1942/6/3")]
        [InlineData("1942.6", "1942/6/1")]
        [InlineData("1942", "1942/1/1")]
        public void DeserializeDates(string data, DateTime expected)
        {
            DateTime actual = ParadoxParser.Deserialize<DateTime>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeNullable()
        {
            int? actual = ParadoxParser.Deserialize<int?>("1".ToStream());
            Assert.Equal(1, actual);
        }

        [Fact]
        public void DeserializeStringArray()
        {
            string data = "{me {you them} us} #hehe";
            string[] expected = { "me", "you", "them", "us" };
            string[] actual = ParadoxParser.Deserialize<string[]>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeIntArray()
        {
            string data = "{1, 2 -1, 0 3}";
            int[] expected = { 1, 2, -1, 0, 3 };
            int[] actual = ParadoxParser.Deserialize<int[]>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeStringCollection()
        {
            string data = "{me {you them} us} #hehe";
            ICollection<string> expected = new[] { "me", "you", "them", "us" };
            ICollection<string> actual = ParadoxParser.Deserialize<ICollection<string>>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeStringIEnumerable()
        {
            string data = "{me {you them} us} #hehe";
            IEnumerable<string> expected = new[] { "me", "you", "them", "us" };
            var actual = ParadoxParser.Deserialize<IEnumerable<string>>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeStringList()
        {
            string data = "{me {you them} us} #hehe";
            var expected = new List<string> { "me", "you", "them", "us" };
            var actual = ParadoxParser.Deserialize<List<string>>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeDictionary()
        {
            string data = "{me=1.000 you=2.500} #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<Dictionary<string, double>>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeIDictionary()
        {
            string data = "{me=1.000 you=2.500} #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<IDictionary<string, double>>(data.ToStream());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeserializeWithoutInitialCurly()
        {
            string data = "me=1.000 you=2.500 #hehe";
            var expected = new Dictionary<string, double> { { "me", 1.000 }, { "you", 2.500 } };
            var actual = ParadoxParser.Deserialize<IDictionary<string, double>>(data.ToStream());
            Assert.Equal(expected, actual);
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

        [Fact]
        public void DeserializeObjects()
        {
            string data = "{value=\"Hey\" bar={id=4}}";
            var actual = ParadoxParser.Deserialize<Foo>(data.ToStream());
            Assert.Equal("Hey", actual.value);
            Assert.Equal(4, actual.bar.id);
        }

        public class Foo2 : IParadoxRead
        {
            public int value { get; set; }
            public void TokenCallback(ParadoxParser parser, string token)
            {
                value = parser.ReadInt32() + 1;
            }
        }

        [Fact]
        public void DeserializeIParadoxReads()
        {
            string data = "{value=4 value=5}";
            var actual = ParadoxParser.Deserialize<Foo2>(data.ToStream());
            Assert.Equal(6, actual.value);
        }

        public class FooAlias
        {
            [ParadoxAlias("Bar")]
            public string Name { get; set; }
        }

        [Fact]
        public void DeserializeAliases()
        {
            var data = "{Bar=You}";
            var actual = ParadoxParser.Deserialize<FooAlias>(data.ToStream());
            Assert.Equal("You", actual.Name);
        }

        public class FooNamingConvention
        {
            public double PapalInfluence { get; set; }
        }

        [Fact]
        public void DeserializeNamingConvention()
        {
            var data = "{papal_influence=2.500}";
            var actual = ParadoxParser.Deserialize<FooNamingConvention>(data.ToStream());
            Assert.Equal(2.500, actual.PapalInfluence);
        }

/*        [Fact]
        public void DeserializeParseTemplate()
        {
            var actual = ParadoxParser.Parse(
                File.OpenRead("FileParseTemplate.txt"), new Province());
            Assert.Equal("My Prov", actual.Name);
            Assert.Equal(1.000, actual.Tax);
            Assert.Equal(new[] { "MEE", "YOU", "THM" }, actual.Cores);
            Assert.Equal(new[] { "BNG", "ORI", "PEG" }, actual.TopProvinces);
            Assert.Equal(1, actual.Armies.Count);
            var army = actual.Armies[0];
            Assert.Equal("My first army", army.Name);
            Assert.Equal(5, army.Leader.Id);
            Assert.Equal(2, army.Units.Count);
            Assert.Equal("First infantry of Awesomeness", army.Units[0].Name);
            Assert.Equal("ninjas", army.Units[0].Type);
            Assert.Equal(5.445, army.Units[0].Morale);
            Assert.Equal(0.998, army.Units[0].Strength);

            Assert.Equal("Second infantry of awesomeness", army.Units[1].Name);
            Assert.Equal("ninjas", army.Units[1].Type);
            Assert.Equal(6.000, army.Units[1].Morale);
            Assert.Equal(1.000, army.Units[1].Strength);

            var act = actual.Armies[0].Attachments;
            Assert.IsNotNull(act);
            Assert.Equal(2, act.Count);
            Assert.Equal(2296, act[0].Id);
            Assert.Equal(54, act[0].Type);
            Assert.Equal(61768, act[1].Id);
            Assert.Equal(4713, act[1].Type);
        }*/
    }
}
