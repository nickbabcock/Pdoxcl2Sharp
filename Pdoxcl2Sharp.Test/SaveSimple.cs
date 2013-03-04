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
        [Test]
        public void SingleSave()
        {
            string input = "culture=michigan";
            string newCulture = "ohio";
            StringWriter save = new StringWriter();

            Action<ParadoxSaver, string> action = (p, s) =>
                {
                    if (s == "culture")
                        p.WriteValue(newCulture);
                };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            Assert.AreEqual("culture=ohio", save.ToString());
        }

        [Test]
        public void SingleQuoteSave()
        {
            string input = "culture=\"michigan\"";
            string newCulture = "ohio";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "culture")
                    p.WriteValue(newCulture, quoteWrap: true);
            };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            Assert.AreEqual("culture=\"ohio\"", save.ToString());
        }

        [Test]
        public void SingleQuoteIgnore()
        {
            string input = "culture=\"michigan\"";
            StringWriter save = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), (p, s) => { });
            StringAssert.Contains(input, save.ToString());
        }

        [Test]
        public void SpaceQuoteIgnore()
        {
            string input = "culture=\"Aztec Patriots\"";
            StringWriter save = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), (p, s) => { });
            StringAssert.Contains(input, save.ToString());
        }

        [Test]
        public void WriteNumericalList()
        {
            List<int> list = new List<int>() { 1, 2, 3 };
            string input = "numbers={ 3 2 1 }";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "numbers")
                    p.WriteList(list, appendNewLine: false);
            };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            Assert.AreEqual("numbers={ 1 2 3 }", save.ToString());
        }

        [Test]
        public void WriteQuotedList()
        {
            List<string> list = new List<string>() { "Infi a", "Infi B" };
            string input = "reg={ }";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "reg")
                    p.WriteList(list, appendNewLine: false, quoteWrap: true);
            };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            Assert.AreEqual("reg={ \"Infi a\" \"Infi B\" }", save.ToString());
        }

        [Test]
        public void WriteTechnologyList()
        {
            List<string> list = new List<string>() { "infantry_theory", "militia_theory", "mobile_theory" };
            string input = "theoretical={ }";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "theoretical")
                    p.WriteList(list, appendNewLine: false, delimiter: Environment.NewLine);
            };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            string expected = "theoretical={\r\n\tinfantry_theory\r\n\tmilitia_theory\r\n\tmobile_theory\r\n}";
            Assert.AreEqual(expected, save.ToString());
        }

        [Test]
        public void IgnoreList()
        {
            string input = "list={1 2 3 4} list2={1 2 3 4}";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
                {
                    if (s == "list2")
                        p.WriteList(new int[] { 4, 3, 2, 1 });
                };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            string expected = "list={1 2 3 4} list2={ 4 3 2 1 }";
            Assert.AreEqual(expected, save.ToString());
        }
        [Test]
        public void IgnoreEmptyList()
        {
            string input = "list={} list={}";
            StringWriter save = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), (p, s) => { });

            Assert.DoesNotThrow(() =>
                ParadoxParser.Parse(save.ToString().ToByteArray(), (p, s) => p.ReadStringList()));

        }
        [Test]
        public void WriteIgnored()
        {
            string input = "culture=michigan\r\ncity=me";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) => { };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            Assert.AreEqual(input + Environment.NewLine, save.ToString());
        }
        [Test]
        public void writeTabbed()
        {
            string input = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) =>
            {
                if (s == "id")
                    p.WriteValue("1", appendNewLine: true);
                else if (s == "type")
                    p.WriteValue("2", appendNewLine: true);
            };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            string expected = "advisor={\r\n\tid=1\r\n\ttype=2\r\n}";
            Assert.AreEqual(expected, save.ToString());
        }
        [Test]
        public void writeIgnoredTabbed()
        {
            string input = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            StringWriter save = new StringWriter();
            Action<ParadoxSaver, string> action = (p, s) => { };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            string expected = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            Assert.AreEqual(expected, save.ToString());
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
            StringWriter save = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), (p, s) => { });
            string output = save.ToString();

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

            StringWriter save = new StringWriter();
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), (p, s) => { });
            Console.WriteLine(save.ToString());
        }
    }
}
