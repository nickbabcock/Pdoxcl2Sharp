using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pdoxcl2Sharp;
using System.IO;

namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class SaveSimple
    {
        private static string runner(string input, Action<ParadoxSaver, string> action)
        {
            StringWriter sw = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(sw, input.ToByteArray(), action);
            return sw.ToString();
        }

        [Test]
        public void SingleSave()
        {
            string input = "culture=michigan";
            string newCulture = "ohio";

            Action<ParadoxSaver, string> action = (p, s) =>
                {
                    if (s == "culture")
                        p.WriteValue(newCulture);
                };
            Assert.AreEqual("culture=ohio", runner(input, action));
        }

        [Test]
        public void SingleQuoteSave()
        {
            string input = "culture=\"michigan\"";
            string newCulture = "ohio";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "culture")
                    p.WriteValue(newCulture, quoteWrap: true);
            };
            Assert.AreEqual("culture=\"ohio\"", runner(input, action));
        }

        [Test]
        public void SingleQuoteIgnore()
        {
            string input = "culture=\"michigan\"";
            StringAssert.Contains(input, runner(input, (p, s) => { }));
        }

        [Test]
        public void SpaceQuoteIgnore()
        {
            string input = "culture=\"Aztec Patriots\"";
            StringAssert.Contains(input, runner(input, (p, s) => { }));
        }

        [Test]
        public void WriteNumericalList()
        {
            List<int> list = new List<int>() { 1, 2, 3 };
            string input = "numbers={ 3 2 1 }";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "numbers")
                    p.WriteList(list, appendNewLine: false);
            };
            Assert.AreEqual("numbers={ 1 2 3 }", runner(input, action));
        }

        [Test]
        public void WriteQuotedList()
        {
            List<string> list = new List<string>() { "Infi a", "Infi B" };
            string input = "reg={ }";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "reg")
                    p.WriteList(list, appendNewLine: false, quoteWrap: true);
            };
            Assert.AreEqual("reg={ \"Infi a\" \"Infi B\" }", runner(input, action));
        }

        [Test]
        public void WriteTechnologyList()
        {
            List<string> list = new List<string>() { "infantry_theory", "militia_theory", "mobile_theory" };
            string input = "theoretical={ }";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "theoretical")
                    p.WriteList(list, appendNewLine: false, delimiter: Environment.NewLine);
            };
            string expected = "theoretical={\r\n\tinfantry_theory\r\n\tmilitia_theory\r\n\tmobile_theory\r\n}";
            Assert.AreEqual(expected, runner(input, action));
        }

        [Test]
        public void IgnoreList()
        {
            string input = "list={1 2 3 4} list2={1 2 3 4}";
            Action<ParadoxSaver, string> action = (p, s) =>
                {
                    if (s == "list2")
                        p.WriteList(new int[] { 4, 3, 2, 1 });
                };
            string expected = "list={1 2 3 4} list2={ 4 3 2 1 }";
            Assert.AreEqual(expected, runner(input, action));
        }
        [Test]
        public void IgnoreEmptyList()
        {
            string input = "list={} list={}";
            string save = runner(input, (p, s) => { }); ;

            Assert.DoesNotThrow(() =>
                ParadoxParser.Parse(save.ToString().ToByteArray(), (p, s) => p.ReadStringList()));

        }
        [Test]
        public void WriteIgnored()
        {
            string input = "culture=michigan\r\ncity=me";
            Assert.AreEqual(input + Environment.NewLine, runner(input, (p, s) =>{}));
        }
        [Test]
        public void writeTabbed()
        {
            string input = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "id")
                    p.WriteValue("1", appendNewLine: true);
                else if (s == "type")
                    p.WriteValue("2", appendNewLine: true);
            };
            string expected = "advisor={\r\n\tid=1\r\n\ttype=2\r\n}";
            Assert.AreEqual(expected, runner(input, action));
        }
        [Test]
        public void writeIgnoredTabbed()
        {
            string input = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            string expected = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            Assert.AreEqual(expected, runner(input, (p,s) => {}));
        }

        [Test]
        public void EquivalentTechWrite()
        {
            string input =
@"	technology=
	{
		land_tech={22 1396.431}
		naval_tech={22 774.762}
    }";
            Assert.That(equivalentSemantics(input));
        }

        [Test]
        public void EquivalentSettingsWrite()
        {
            string input =
@"gameplaysettings=
{
	setgameplayoptions=
	{
0 0 0 0 0 0 0 2 0 1 1 0 0 	}
}
start_date=""1399.10.14""";
            Assert.That(equivalentSemantics(input));
        }

        [Test]
        public void EquivalentBeginningWrite()
        {
            string input =
@"date=""1605.3.14""
player=""NAJ""
monarch=10031";
            Assert.That(equivalentSemantics(input));
        }

        private static bool equivalentSemantics(string input)
        {
            string output = runner(input, (p, s) => { });

            List<string> first = new List<string>();
            List<int> firstInd = new List<int>();
            List<string> second = new List<string>();
            List<int> secondInd = new List<int>();
            ParadoxParser.Parse(input.ToByteArray(), (p, s) =>
                {
                    first.Add(s);
                    firstInd.Add(p.CurrentIndex);
                });
            ParadoxParser.Parse(output.ToByteArray(), (p, s) =>
                {
                    second.Add(s);
                    secondInd.Add(p.CurrentIndex);
                });

            return first.SequenceEqual(second) && firstInd.SequenceEqual(secondInd);
        }

        [Test]
        public void ReadAndSaveForeign()
        {
            string input = @"name=""Vjenceslav Draškovic""";
            string expected = "Vjenceslav Draškovic";
            string actual = string.Empty;
            ParadoxParser.Parse(input.ToByteArray(), (p, s) => actual = p.ReadString());
            Assert.AreEqual(expected, actual);

            CollectionAssert.AreEqual(expected.ToCharArray(), runner(expected, (p, s) => { }).Trim());
        }

        [Test]
        public void ReadAndSaveForeign2()
        {
            string input = @"name=""Östergötland""";
            string expected = "Östergötland";
            string actual = string.Empty;
            ParadoxParser.Parse(input.ToByteArray(), (p, s) => actual = p.ReadString());
            Assert.AreEqual(expected, actual);

            CollectionAssert.AreEqual(expected.ToCharArray(), runner(expected, (p, s) => { }).Trim());
        }

        [Test]
        public void SaveHeader()
        {
            string input = "1={\r\n\tname1=value1\r\n}";
            string expected = "1={\r\n\tname1=value2\r\n}\r\n";
            Action<ParadoxSaver, string> action = (p,s) =>
                {
                    if (s == "1")
                    {
                        p.Parse((p2, s2) =>
                            {
                                if (s2 == "name1")
                                    p2.WriteValue("value2", appendNewLine: true, quoteWrap: false);
                            });
                    }
                };
            string actual = runner(input, action);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SaverSkipper()
        {
            string input = "core=VEN\r\ncore=ABB\r\nVEN=name1";
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "core")
                {
                    p.SkipNext();
                }
                else if (s == "VEN")
                {
                    p.WriteValue("name2", appendNewLine: true);
                    foreach (string str in new string[] { "ABB", "VEN" })
                        p.Write(string.Format("core={0}", str), appendNewLine: true);

                }
            };
            string actual = runner(input, action);
            string expected = "VEN=name2\r\ncore=ABB\r\ncore=VEN\r\n";
            Assert.AreEqual(expected, actual);
        }
    }
}
