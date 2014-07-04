using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class Bracket
    {
        [Test]
        public void BracketTest()
        {
            string input = @"id={
	type=40
}
id=21016
name=""test""";
            string typeVal = string.Empty;
            string idVal = string.Empty;
            string nameVal = string.Empty;
            Action<ParadoxParser, string> action = (p, s) =>
            {
                p.NextIsBracketed();
                
                if (p.NextIsBracketed())
                {
                    Action<ParadoxParser, string> innerAction = (p2, s2) =>
                    {
                        if (s2 == "type")
                            typeVal = p2.ReadString();
                    };
                    p.Parse(innerAction);
                } else
                {
                    if(s == "id")
                        idVal = p.ReadString();
                    else if(s == "name")
                        nameVal = p.ReadString();
                }
            };
            ParadoxParser.Parse(input.ToStream(), action);
            Assert.AreEqual("21016", idVal);
            Assert.AreEqual("40", typeVal);
            Assert.AreEqual("test", nameVal);
        }

        [Test]
        public void BracketSpaceTest()
        {
            string input = @"id = {
	type = 40
}
id = 21016
name=""test""";
            string typeVal = string.Empty;
            string idVal = string.Empty;
            string nameVal = string.Empty;
            Action<ParadoxParser, string> action = (p, s) =>
            {
                p.NextIsBracketed();
                
                if (p.NextIsBracketed())
                {
                    Action<ParadoxParser, string> innerAction = (p2, s2) =>
                    {
                        if (s2 == "type")
                            typeVal = p2.ReadString();
                    };
                    p.Parse(innerAction);
                } else
                {
                    if(s == "id")
                        idVal = p.ReadString();
                    else if(s == "name")
                        nameVal = p.ReadString();
                }
            };
            ParadoxParser.Parse(input.ToStream(), action);
            Assert.AreEqual("21016", idVal);
            Assert.AreEqual("40", typeVal);
            Assert.AreEqual("test", nameVal);
        }
        
        [Test]
        public void SingleBracket()
        {
            string toParse = "date={date2=\"1770.12.5\"}";
            var data = toParse.ToStream();

            Dater actual = new Dater();
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x => x.Parse(actual)}
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());
            Assert.AreEqual(new DateTime(1770, 12, 5), actual.Date);            
        }

        [Test]
        public void MultipleBracket()
        {
            var data = ("date={date2=\"1770.12.5\"}" + Environment.NewLine +
                       "date={date2=\"1666.6.6\"}").ToStream();
            List<Dater> actual = new List<Dater>();
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x =>  {
                    Dater newDate = new Dater();
                    x.Parse(newDate);
                    actual.Add(newDate);
                }}
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());

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
            var data = "\t\tland_tech={45 1020.600}".ToStream();

            int? tech = null ;
            double? progress = null;

            IDictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"land_tech", x => x.ReadInsideBrackets(parser => ReadInto(parser, ref tech, ref progress))}
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());
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
}".ToStream();

            Dictionary<int, byte> expected = new Dictionary<int, byte>()
            {
                {13214, 31},
                {13657, 31},
                {11566, 31},
                {10237, 31}
            };

            TestDictionary(data, x => x.ReadDictionary(p => p.ReadInt32(), p => p.ReadByte()), expected, "low_pressure_zones");
        }

        [Test]
        public void MapDataNoSpace()
        {
            var data = @"low_pressure_zones={
	1321=31
	13657=31
	11566=31
	10237=31}".ToStream();

            Dictionary<int, byte> expected = new Dictionary<int, byte>()
            {
                {1321, 31},
                {13657, 31},
                {11566, 31},
                {10237, 31}
            };

            TestDictionary(data, x => x.ReadDictionary(p => p.ReadInt32(), p => p.ReadByte()), expected, "low_pressure_zones");
        }

        [Test]
        public void EmptyMap()
        {
            var data = "low_pressure_zones={}".ToStream();
            TestDictionary(data, x => x.ReadDictionary(p => p.ReadInt32(), p => p.ReadByte()), 
                Enumerable.Empty<KeyValuePair<int, byte>>(), "low_pressure_zones");
        }

        [Test]
        public void EmptySpacedMap()
        {
            var data = "low_pressure_zones  =   {    }     ".ToStream();
            TestDictionary(data, x => x.ReadDictionary(p => p.ReadInt32(), p => p.ReadByte()), 
                Enumerable.Empty<KeyValuePair<int, byte>>(), "low_pressure_zones");
        }


        private void TestDictionary<K, V>(Stream data, Func<ParadoxParser, IDictionary<K, V>> func, IEnumerable<KeyValuePair<K, V>> expected, string tokenStr)
        {
            IDictionary<K, V> actual = null;

            Action<ParadoxParser, string> action = (p, token) =>
                {
                    if (token == tokenStr)
                        actual = func(p);
                };

            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEquivalent(expected, actual);
        }
    }


    class Dater : Pdoxcl2Sharp.IParadoxRead
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
