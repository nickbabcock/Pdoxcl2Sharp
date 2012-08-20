using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class Bracket
    {
        [Test]
        public void SingleBracket()
        {
            string toParse = "date={date2=\"1770.12.5\"}";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();

            Dater actual = new Dater();
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x => x.Parse(actual)}
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());
            Assert.AreEqual(new DateTime(1770, 12, 5), actual.Date);            
        }

        [Test]
        public void MultipleBracket()
        {
            var data = ("date={date2=\"1770.12.5\"}" + Environment.NewLine +
                       "date={date2=\"1666.6.6\"}").ToByteArray();
            List<Dater> actual = new List<Dater>();
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x =>  {
                    Dater newDate = new Dater();
                    x.Parse(newDate);
                    actual.Add(newDate);
                }}
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            List<DateTime> expected = new List<DateTime>
            {
                new DateTime(1770,12,5),
                new DateTime(1666,6,6)
            };

            CollectionAssert.AreEquivalent(expected, actual.Select(x => x.Date));
        }

        void ReadInto(ParadoxParser x, ref int? tech, ref double? progress)
        {
            tech = x.ReadInt32();
            progress = x.ReadDouble();
        }

        [Test]
        public void TechnologyBracket()
        {
            var data = "\t\tland_tech={45 1020.600}".ToByteArray();

            int? tech = null ;
            double? progress = null;

            IDictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"land_tech", x => x.ReadInsideBrackets(parser => ReadInto(parser, ref tech, ref progress))}
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());
            Assert.That(tech.HasValue);
            Assert.That(progress.HasValue);
            Assert.AreEqual(45, tech);
            Assert.AreEqual(1020.600, progress);
        }

        [Test]
        public void MapData()
        {
            var data = @"low_pressure_zones = {
	13214	= 31 #icelandinc
	13657	= 31 #aleutian
	11566	= 31 #central pacific
	10237	= 31 #brazil
}".ToByteArray();

            IDictionary<int, byte> maps = new Dictionary<int, byte>();

            Action<ParadoxParser, string> action = (parser, s) =>
            {
                if (s == "low_pressure_zones")
                {
                    maps = parser.ReadDictionary(x => x.ReadInt32(), x => x.ReadByte());
                }
            };

            ParadoxParser.Parse(data, action);

            Dictionary<int, byte> expected = new Dictionary<int, byte>()
            {
                {13214, 31},
                {13657, 31},
                {11566, 31},
                {10237, 31}
            };

            CollectionAssert.AreEquivalent(expected, maps);
        }

        [Test]
        public void MapDataNoSpace()
        {
            var data = @"low_pressure_zones={
	1321=31
	13657=31
	11566=31
	10237=31}".ToByteArray();

            IDictionary<int, byte> maps = new Dictionary<int, byte>();

            Action<ParadoxParser, string> action = (parser, s) =>
            {
                if (s == "low_pressure_zones")
                {
                    maps = parser.ReadDictionary(x => x.ReadInt32(), x => x.ReadByte());
                }
            };

            ParadoxParser.Parse(data, action);

            Dictionary<int, byte> expected = new Dictionary<int, byte>()
            {
                {1321, 31},
                {13657, 31},
                {11566, 31},
                {10237, 31}
            };

            CollectionAssert.AreEquivalent(expected, maps);
        }

        [Test]
        public void EmptyMap()
        {
            var data = "low_pressure_zones={}".ToByteArray();
            IDictionary<int, byte> maps = new Dictionary<int, byte>();

            Action<ParadoxParser, string> action = (parser, s) =>
            {
                if (s == "low_pressure_zones")
                {
                    maps = parser.ReadDictionary(x => x.ReadInt32(), x => x.ReadByte());
                }
            };

            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEquivalent(Enumerable.Empty<KeyValuePair<int, byte>>(), maps);
        }

        [Test]
        public void EmptySpacedMap()
        {
            var data = "low_pressure_zones  =   {    }     ".ToByteArray();
            IDictionary<int, byte> maps = new Dictionary<int, byte>();

            Action<ParadoxParser, string> action = (parser, s) =>
            {
                if (s == "low_pressure_zones")
                {
                    maps = parser.ReadDictionary(x => x.ReadInt32(), x => x.ReadByte());
                }
            };

            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEquivalent(Enumerable.Empty<KeyValuePair<int, byte>>(), maps);
        }

    }


    class Dater : Pdoxcl2Sharp.IParadoxFile
    {
        private IDictionary<string, Action<ParadoxParser>> parseValues;
        public Dater()
        {
            parseValues = new Dictionary<string, Action<ParadoxParser>>
            {
                { "date2", x => Date = x.ReadDateTime()}
            };
        }

        public DateTime Date { get; set; }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            Action<ParadoxParser> temp;
            if (parseValues.TryGetValue(token, out temp))
                temp(parser);
        }
    }

}
