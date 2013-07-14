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
    class Saving
    {
        [Test]
        public void SingleSaveNewLine()
        {
            StringWriter sw = new StringWriter();
            ParadoxSaver saver = new ParadoxSaver(sw);
            saver.Write("culture", "michigan");
            Assert.AreEqual("culture=michigan", sw.ToString());
        }

        [Test]
        public void SingleQuoteSaveNewLine()
        {
            StringWriter sw = new StringWriter();
            ParadoxSaver saver = new ParadoxSaver(sw);
            saver.Write("culture", "michigan", ValueWrite.Quoted);
            Assert.AreEqual("culture=\"michigan\"", sw.ToString());
        }

        [Test]
        public void SaveComment()
        {
            StringWriter sw = new StringWriter();
            ParadoxSaver saver = new ParadoxSaver(sw);
            saver.WriteComment("This is a comment");
            Assert.AreEqual("#This is a comment" + sw.NewLine, sw.ToString());
        }

        [Test]
        public void SaveNonUtf8Characters()
        {
            string actual;
            using (MemoryStream ms = new MemoryStream())
            using (StreamWriter sw = new StreamWriter(ms, Globals.ParadoxEncoding))
            {
                ParadoxSaver saver = new ParadoxSaver(sw);
                saver.Write("name", "šžŸ", ValueWrite.None);
                sw.Flush();

                actual = sw.Encoding.GetString(ms.ToArray());
            }
            Assert.AreEqual("name=šžŸ", actual);
        }
    }
}
