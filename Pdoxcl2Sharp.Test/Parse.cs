using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pdoxcl2Sharp;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    public class Parse
    {
        [Test]
        public void ReadSingleString()
        {
            var data = "michigan".ToStream();
            string actual = string.Empty;
            Action<ParadoxParser, string> action = (x, s) => actual = s;
            ParadoxParser.Parse(data, action);
            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void ReadSingleSpacedString()
        {
            var data = "   michigan    ".ToStream();
            string actual = string.Empty;
            Action<ParadoxParser, string> action = (x, s) => actual = s;
            ParadoxParser.Parse(data, action);
            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void Simple()
        {
            string toParse = "culture=michigan";
            var data = toParse.ToStream();


            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void SimpleWithSpace()
        {
            var data = "culture = michigan".ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void SimpleName()
        {
            string toParse = "name=\"Nick\"";
            var data = toParse.ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "name", x => actual = x.ReadString() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("Nick", actual);
        }

        [Test]
        public void PartialQuotedName()
        {
            string toParse = "name=\"Nick\"_name";
            var data = toParse.ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "name", x => actual = x.ReadString() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("Nick_name", actual);
        }

        [Test]
        public void SimpleComment()
        {
            string toParse = "#culture=michigan";
            var data = toParse.ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());
            Assert.AreEqual(String.Empty, actual);
        }

        [Test]
        public void FollowingCommentNoSpace()
        {
            string toParse = "tag = data#culture=michigan";
            var data = toParse.ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());
            Assert.AreEqual(String.Empty, actual);
        }

        [Test]
        public void FollowingCommentWithSpace()
        {
            string toParse = "tag = data #culture=michigan";
            var data = toParse.ToStream();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());
            Assert.AreEqual(String.Empty, actual);
        }

        [Test]
        public void SimpleInt32()
        {
            string toParse = "ID=130";
            var data = toParse.ToStream();

            int actual = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual(130, actual);
        }
        [Test]
        public void NegativeInt32()
        {
            var data = "ID=-130".ToStream();
            int actual = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual(-130, actual);
        }

        [Test]
        public void Int32WithSpace()
        {
            var data = "ID = -130".ToStream();
            int actual = 0;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual(-130, actual);
        }

        [Test]
        public void BooleanParse()
        {
            var data = "cool=yes".ToStream();
            bool isCool = false;
            ParadoxParser.Parse(data, (p, s) => isCool = p.ReadBool());
            Assert.AreEqual(true, isCool);
        }

        [Test]
        public void BooleanParse2()
        {
            var data = "cool=no".ToStream();
            bool isCool = true;
            ParadoxParser.Parse(data, (p, s) => isCool = p.ReadBool());
            Assert.AreEqual(false, isCool);
        }


        [Test]
        public void BooleanParse3()
        {
            var data = "cool={ 1.0 }".ToStream();
            Assert.Throws<ApplicationException>(() =>
                ParadoxParser.Parse(data, (p, s) => p.ReadBool()));
        }

        [Test]
        public void TrickyNewLine()
        {
            var data = "tag=tagger\ntype=typer".ToStream();
            string tag = string.Empty;
            string type = String.Empty;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "tag", x => tag = x.ReadString() },
                { "type", x => type = x.ReadString() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("tagger", tag);
            Assert.AreEqual("typer", type);
        }

        [Test]
        public void TrickyNewLineWithQuotes()
        {
            var data = "name=\"namer1\"\ncolor=\"Gray\"".ToStream();

            string name = string.Empty;
            string color = string.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"name", x => name = x.ReadString()},
                {"color", x => color = x.ReadString()}
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.AreEqual("namer1", name);
            Assert.AreEqual("Gray", color);
        }

        [Test]
        public void ExtraNewLinesDontMatter()
        {
            var data = "\n\n\n\n ID=100 \n\n\n\n\n\n".ToStream();

            int id = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"ID", x => id = x.ReadInt32()}
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());
            Assert.AreEqual(100, id);

        }

        [Test]
        public void SimpleDate()
        {
            string toParse = "date=\"1770.12.5\"";
            var data = toParse.ToStream();

            DateTime actual = DateTime.MinValue;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "date", x => actual = x.ReadDateTime() }
            };
            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            DateTime expected = new DateTime(1770, 12, 5);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void HourlyDate()
        {
            string toParse = "date=\"1936.1.10.4\"";
            DateTime actual = DateTime.MinValue;
            ParadoxParser.Parse(toParse.ToStream(), (p, s) => { if (s == "date") actual = p.ReadDateTime(); });
            DateTime expected = new DateTime(1936, 1, 10, 4, 0, 0);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SimpleMultiLine()
        {
            string toParse = "date=\"1770.12.5\"" + Environment.NewLine +
                             "player=\"JAP\"" + Environment.NewLine +
                             "monarch=12209";

            var data = toParse.ToStream();
            DateTime? actualDate = null;
            string actualPlayer = null;
            int? actualMonarch = null;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "date", x => actualDate = x.ReadDateTime() },
                { "player", x => actualPlayer = x.ReadString() },
                { "monarch", x => actualMonarch = x.ReadInt32() }
            };

            ParadoxParser.Parse(data, dictionary.ParserAdapter());

            Assert.That(actualDate.HasValue);
            Assert.That(!String.IsNullOrEmpty(actualPlayer));
            Assert.That(actualMonarch.HasValue);

            Assert.AreEqual(new DateTime(1770, 12, 5), actualDate);
            Assert.AreEqual("JAP", actualPlayer);
            Assert.AreEqual(12209, actualMonarch);
        }

        [Test]
        public void NoEmptyStringStatic()
        {
            var data = "A B C D ".ToStream();
            var expected = new[] { "A", "B", "C", "D" };
            List<string> actual = new List<string>();
            ParadoxParser.Parse(data, (p, s) => actual.Add(s));
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void WhenChristWasBorn()
        {
            string input = "date=1.1.1";
            DateTime expected = new DateTime(1, 1, 1);
            DateTime actual = DateTime.MaxValue;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x => actual = x.ReadDateTime()}
            };
            ParadoxParser.Parse(input.ToStream(), dictionary.ParserAdapter());
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FirstMilleniumParty()
        {
            string input = "date=999.1.1";
            DateTime expected = new DateTime(999, 1, 1);
            DateTime actual = DateTime.MaxValue;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"date", x => actual = x.ReadDateTime()}
            };
            ParadoxParser.Parse(input.ToStream(), dictionary.ParserAdapter());
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void NestedParse()
        {
            string input = @"rebel_faction=
{
	id=
	{
		id=21016
		type=40
	}
	type=""nationalist_rebels""
}";
            string actual = string.Empty;
            Action<ParadoxParser, string> action = (p, s) =>
                {
                    if (s == "rebel_faction")
                    {
                        Action<ParadoxParser, string> innerAction = (p2, s2) =>
                            {
                                if (p2.CurrentIndent == 1 && s2 == "type")
                                    actual = p2.ReadString();
                            };
                        p.Parse(innerAction);
                    }
                };
            ParadoxParser.Parse(input.ToStream(), action);
            Assert.AreEqual("nationalist_rebels", actual);
        }


        [Test]
        public void NestedParseAfter()
        {
            string input = @"rebel_faction=
{
	id=
	{
		id=21016
		type=40
	}
	type=""nationalist_rebels""
    army=
    {
        id=1
        type=1
    }
}
me=you";
            string actual = string.Empty;
            string meActual = string.Empty;
            Action<ParadoxParser, string> action = (p, s) =>
                {
                    if (s == "rebel_faction")
                    {
                        Action<ParadoxParser, string> innerAction = (p2, s2) =>
                            {
                                if (p2.CurrentIndent == 1 && s2 == "type")
                                    actual = p2.ReadString();
                            };
                        p.Parse(innerAction);
                    }
                    else if (s == "me")
                        meActual = p.ReadString();
                };
            ParadoxParser.Parse(input.ToStream(), action);
            Assert.AreEqual("nationalist_rebels", actual);
            Assert.AreEqual("you", meActual);
        }


        public class Cid : IParadoxRead
        {
            public int Id { get; set; } 
            public int Type { get; set; }

            public void TokenCallback(ParadoxParser parser, string token)
            {
                if (token == "id") Id = parser.ReadInt32();
                else if (token == "type") Type = parser.ReadInt32();
            }
        }

        [Test]
        public void ParseListObject()
        {
            var data = @"attachments={
			{
				id=2296
				type=54
			}

			{
				id=61768
				type=4713
			}
}";

            IList<Cid> actual = null;
            ParadoxParser.Parse(data.ToStream(), (p, s) =>
                {
                    if (s == "attachments")
                    {
                        actual = p.ReadList(() => p.Parse(new Cid()));
                    }
                });

            Assert.IsNotNull(actual);
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(2296, actual[0].Id);
            Assert.AreEqual(54, actual[0].Type);
            Assert.AreEqual(61768, actual[1].Id);
            Assert.AreEqual(4713, actual[1].Type);
        }

        public class TestNode : IParadoxRead
        {
            public IList<Incoming> incomings;
            public IList<double> tradeGoodsSize;
            public TestNode()
            {
                incomings = new List<Incoming>();
            }

            public void TokenCallback(ParadoxParser parser, string token)
            {
                if (token == "incoming")
                    incomings.Add(parser.Parse(new Incoming()));
                else if (token == "trade_goods_size")
                    tradeGoodsSize = parser.ReadDoubleList();
                else
                    throw new ApplicationException(token + " not recognized");
            }
        }

        public class Incoming : IParadoxRead
        {
            public int actualAddedValue;
            public int value;
            public int from;
            public Cid modifier;
            public void TokenCallback(ParadoxParser parser, string token)
            {
                switch (token)
                {
                    case "actual_added_value": actualAddedValue = parser.ReadInt32(); break;
                    case "value": value = parser.ReadInt32(); break;
                    case "from": from = parser.ReadInt32(); break;
                    case "modifier": modifier = parser.Parse(new Cid()); break;
                }
            }
        }


        [Test]
        public void ListRegression()
        {
            var data = @"trade=
{
    node=
    {
		incoming=
		{
			actual_added_value=0
			value=0
			from=1
            modifier=
            {
                id=3
                type=49
            }
		}
		trade_goods_size=
		{
0.000 0.710 }
    }
}";
            List<TestNode> nodes = new List<TestNode>();
            ParadoxParser.Parse(data.ToStream(), (p, s) =>
                {
                    if (s == "node")
                        nodes.Add(p.Parse(new TestNode()));
                });

            Assert.That(nodes.Count, Is.EqualTo(1));
            var actual = nodes[0];
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.incomings, Is.Not.Null);
            Assert.That(actual.incomings.Count, Is.EqualTo(1));
            Assert.That(actual.incomings[0].actualAddedValue, Is.EqualTo(0));
            Assert.That(actual.incomings[0].value, Is.EqualTo(0));
            Assert.That(actual.incomings[0].from, Is.EqualTo(1));
            Assert.That(actual.tradeGoodsSize, Is.EquivalentTo(new[] { 0, 0.710 }));
        }

        [Test]
        public void EmptyBrackets()
        {
            var data = @"history=
{
	1452.4.9=
	{
		controller=
		{
			controller2=""SWE""
		}
	}

	{
	}
}
patrol=1";
            int? patrol = null;
            string controller2 = null;
            ParadoxParser.Parse(data.ToStream(), (p, s) =>
                {
                    if (s == "history")
                        p.Parse((p2, s2) =>
                        {
                            switch(s2)
                            {
                                case "controller2": controller2 = p.ReadString(); break;
                                case "1452.4.9":
                                case "controller":
                                    break;
                                default:
                                    throw new ApplicationException("Unrecognized Token");
                            }
                        });
                    else if (s == "patrol")
                        patrol = p.ReadInt32();
                    else
                        throw new ApplicationException("Unrecognized Token");
                });

            Assert.That(patrol, Is.Not.Null);
            Assert.That(patrol, Is.EqualTo(1));
            Assert.That(controller2, Is.Not.Null);
            Assert.That(controller2, Is.EqualTo("SWE"));
        }
    }

}
