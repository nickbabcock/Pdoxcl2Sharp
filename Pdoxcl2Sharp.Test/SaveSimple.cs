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
        //[Test]
        //public void tabbedSingleSave()
        //{
        //    string input = "\tculture=michigan";
        //    string newCulture = "ohio";
        //    StringWriter save = new StringWriter();

        //    Action<ParadoxSaver, string> action = (p, s) =>
        //    {
        //        if (s == "culture")
        //            p.WriteValue(newCulture);
        //    };
        //    ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
        //    Assert.AreEqual("\tculture=ohio", save.ToString()); 
        //}
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
            Action<ParadoxSaver, string> action = (p, s) => {  };
            ParadoxSaver t = new ParadoxSaver(save, input.ToByteArray(), action);
            string expected = "advisor={\r\n\tid=1562\r\n\ttype=39\r\n}";
            Assert.AreEqual(expected, save.ToString());
        }
    }
}
