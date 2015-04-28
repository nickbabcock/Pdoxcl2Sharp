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
                Assert.AreEqual("#This is a comment\r\n", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Test]
        public void SaveDate()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.WriteLine("date", new DateTime(1, 1, 1));
                }
                Assert.AreEqual("date=\"1.1.1\"\r\n", Globals.ParadoxEncoding.GetString(ms.ToArray()));
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

        [Test]
        public void SaveDouble()
        {
            string actual;
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write(0.23456);
                }
                actual = Globals.ParadoxEncoding.GetString(ms.ToArray());
                Assert.AreEqual("0.235", actual);
            }
        }

        [Test]
        public void SaveInt()
        {
            string actual;
            int val = -1235346;
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write(val);
                }
                actual = Globals.ParadoxEncoding.GetString(ms.ToArray());
                Assert.AreEqual("-1235346", actual);
            }
        }

        [Test]
        public void SaveDatetime()
        {
            string actual;
            DateTime value = new DateTime(1402, 1, 1);
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write(value);
                }
                actual = Globals.ParadoxEncoding.GetString(ms.ToArray());
                Assert.AreEqual("1402.1.1", actual);
            }
        }

        [Test]
        public void SaveWithoutHeader()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write(String.Empty, (w) =>
                        {
                            w.WriteLine("Hey there!", ValueWrite.LeadingTabs);
                        });
                }
                string actual = Globals.ParadoxEncoding.GetString(ms.ToArray());
                Assert.AreEqual("{\r\n\tHey there!\r\n}\r\n", actual);
            }
        }
    }
}
