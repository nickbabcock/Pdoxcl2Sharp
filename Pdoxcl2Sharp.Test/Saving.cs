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
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write("culture", "michigan");
                }
                Assert.AreEqual("culture=michigan", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Test]
        public void SingleQuoteSaveNewLine()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write("culture", "michigan", ValueWrite.Quoted);
                }
                Assert.AreEqual("culture=\"michigan\"", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Test]
        public void SaveComment()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.WriteComment("This is a comment");
                }
                Assert.AreEqual("#This is a comment" + Environment.NewLine, Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Test]
        public void SaveNonUtf8Characters()
        {
            string actual;
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write("name", "šžŸ", ValueWrite.None);
                }
                actual = Globals.ParadoxEncoding.GetString(ms.ToArray());
                Assert.AreEqual("name=šžŸ", actual);
            }
        }
    }
}
