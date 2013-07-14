using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pdoxcl2Sharp;
using NUnit.Framework;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class Building : IParadoxRead
    {
        string onCompletion;
        float completionSize;
        byte cost;
        ushort time;
        bool onMap;
        byte maxLevel;

        Dictionary<string, string> otherFields;

        IDictionary<string, Action<ParadoxParser>> parseStrategy;
        [Test]
        public void ParseCorrectly()
        {
            parseStrategy = new Dictionary<string, Action<ParadoxParser>>
            {
                {"on_completion", x => onCompletion = x.ReadString()},
                {"completion_size", x => completionSize = x.ReadFloat()},
                {"cost", x => cost = x.ReadByte()},
                {"time", x => time = x.ReadUInt16()},
                {"onmap", x => onMap = x.ReadString() == "yes"},
                {"max_level", x => maxLevel = x.ReadByte()}
            };
            otherFields = new Dictionary<string, string>();
            var data = @"on_completion = construction_practical
	completion_size = 0.13

	air_capacity = 1
	capital = yes
	onmap = yes
	cost = 2
	time = 180
	max_level = 10
	visibility = yes".ToStream();
            ParadoxParser p = new ParadoxParser(data, TokenCallback);

            Assert.AreEqual(0.13f, completionSize);
            Assert.AreEqual("construction_practical", onCompletion);
            Assert.AreEqual(true, onMap);
            Assert.AreEqual(2, cost);
            Assert.AreEqual(180, time);
            Assert.AreEqual(10, maxLevel);

            Dictionary<string, string> expected = new Dictionary<string, string>()
            {
                { "air_capacity", "1" },
                { "capital", "yes" },
                { "visibility", "yes" }
            };
            CollectionAssert.AreEqual(expected, otherFields);
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            Action<ParadoxParser> temp;
            if (parseStrategy.TryGetValue(token, out temp))
                temp(parser);
            else
                otherFields.Add(token, parser.ReadString());
        }
    }
}
