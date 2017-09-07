using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pdoxcl2Sharp;
using Xunit;
namespace Pdoxcl2Sharp.Test
{
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
        [Fact]
        public void ParseCorrectly()
        {
            parseStrategy = new Dictionary<string, Action<ParadoxParser>>
            {
                {"on_completion", x => onCompletion = x.ReadString()},
                {"completion_size", x => completionSize = x.ReadFloat()},
                {"cost", x => cost = x.ReadByte()},
                {"time", x => time = x.ReadUInt16()},
                {"onmap", x => onMap = x.ReadBool()},
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
            ParadoxParser.Parse(data, TokenCallback);

            Assert.Equal(0.13f, completionSize);
            Assert.Equal("construction_practical", onCompletion);
            Assert.Equal(true, onMap);
            Assert.Equal(2, cost);
            Assert.Equal(180, time);
            Assert.Equal(10, maxLevel);

            Dictionary<string, string> expected = new Dictionary<string, string>()
            {
                { "air_capacity", "1" },
                { "capital", "yes" },
                { "visibility", "yes" }
            };
            Assert.Equal(expected, otherFields);
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
