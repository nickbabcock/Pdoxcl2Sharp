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
            ParadoxParser p = new ParadoxParser(data, dictionary);

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
            ParadoxParser p = new ParadoxParser(data, dictionary);

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

            ParadoxParser p = new ParadoxParser(data, dictionary);
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
            ParadoxParser p = new ParadoxParser(data, dictionary);

            Assert.AreEqual(130, actual);
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
            ParadoxParser p = new ParadoxParser(data, dictionary);

            DateTime expected = new DateTime(1770, 12, 5);
            Assert.AreEqual(expected, actual);
        }
    }
}
