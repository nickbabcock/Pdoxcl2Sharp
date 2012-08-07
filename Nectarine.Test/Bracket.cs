using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
namespace Nectarine.Test
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

            ParadoxParser p = new ParadoxParser(data, dictionary);
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

            ParadoxParser p = new ParadoxParser(data, dictionary);

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

            ParadoxParser p = new ParadoxParser(data, dictionary);
            Assert.That(tech.HasValue);
            Assert.That(progress.HasValue);
            Assert.AreEqual(45, tech);
            Assert.AreEqual(1020.600, progress);
        }
    }


    class Dater : Nectarine.IParadoxFile
    {
        public IDictionary<string, Action<ParadoxParser>> ParseValues
        {
            get
            {
                return new Dictionary<string, Action<ParadoxParser>>
                {
                    {"date2", x => Date = x.ReadDateTime()}
                };
            }
        }

        public DateTime Date { get; set; }
    }

}
