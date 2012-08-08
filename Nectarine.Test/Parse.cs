using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Nectarine;
namespace Nectarine.Test
{
    [TestFixture]
    public class Parse
    {
        [Test]
        public void Simple()
        {
            string toParse = "culture=michigan";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();


            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void SimpleWithSpace()
        {
            var data = "culture = michigan".ToByteArray();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void SimpleName()
        {
            string toParse = "name=\"Nick\"";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "name", x => actual = x.ReadString() }
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual("Nick", actual);
        }

        [Test]
        public void SimpleComment()
        {
            string toParse = "#culture=michigan";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();

            string actual = String.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "culture", x => actual = x.ReadString() }
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());
            Assert.AreEqual(String.Empty, actual);
        }

        [Test]
        public void SimpleInt32()
        {
            string toParse = "ID=130";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();

            int actual = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual(130, actual);
        }
        [Test]
        public void NegativeInt32()
        {
            var data = "ID=-130".ToByteArray();
            int actual = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual(-130, actual);
        }

        [Test]
        public void Int32WithSpace()
        {
            var data = "ID = -130".ToByteArray();
            int actual = 0;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "ID", x => actual = x.ReadInt32() }
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual(-130, actual);
        }

        [Test]
        public void TrickyNewLine()
        {
            var data = "tag=tagger\ntype=typer".ToByteArray();
            string tag = string.Empty;
            string type = String.Empty;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "tag", x => tag = x.ReadString() },
                { "type", x => type = x.ReadString() }
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual("tagger", tag);
            Assert.AreEqual("typer", type);
        }

        [Test]
        public void TrickyNewLineWithQuotes()
        {
            var data = "name=\"namer1\"\ncolor=\"Gray\"".ToByteArray();

            string name = string.Empty;
            string color = string.Empty;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"name", x => name = x.ReadString()},
                {"color", x => color = x.ReadString()}
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.AreEqual("namer1", name);
            Assert.AreEqual("Gray", color);
        }

        [Test]
        public void ExtraNewLinesDontMatter()
        {
            var data = "\n\n\n\n ID=100 \n\n\n\n\n\n".ToByteArray();

            int id = 0;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                {"ID", x => id = x.ReadInt32()}
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());
            Assert.AreEqual(100, id);

        }

        [Test]
        public void SimpleDate()
        {
            string toParse = "date=\"1770.12.5\"";
            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();

            DateTime actual = DateTime.MinValue;
            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "date", x => actual = x.ReadDateTime() }
            };
            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            DateTime expected = new DateTime(1770, 12, 5);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void SimpleMultiLine()
        {
            string toParse = "date=\"1770.12.5\"" + Environment.NewLine +
                             "player=\"JAP\"" + Environment.NewLine +
                             "monarch=12209";

            var data = toParse.ToCharArray().Select(x => (byte)x).ToArray();
            DateTime? actualDate = null;
            string actualPlayer = null;
            int? actualMonarch = null;

            Dictionary<string, Action<ParadoxParser>> dictionary = new Dictionary<string, Action<ParadoxParser>>
            {
                { "date", x => actualDate = x.ReadDateTime() },
                { "player", x => actualPlayer = x.ReadString() },
                { "monarch", x => actualMonarch = x.ReadInt32() }
            };

            ParadoxParser p = new ParadoxParser(data, dictionary.ParserAdapter());

            Assert.That(actualDate.HasValue);
            Assert.That(!String.IsNullOrEmpty(actualPlayer));
            Assert.That(actualMonarch.HasValue);

            Assert.AreEqual(new DateTime(1770, 12, 5), actualDate);
            Assert.AreEqual("JAP", actualPlayer);
            Assert.AreEqual(12209, actualMonarch);
        }

    }
}
