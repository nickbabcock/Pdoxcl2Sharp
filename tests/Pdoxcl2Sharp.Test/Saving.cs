using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Pdoxcl2Sharp;
using System.IO;

namespace Pdoxcl2Sharp.Test
{
    public class Saving
    {
        [Fact]
        public void SingleSaveNewLine()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write("culture", "michigan");
                }
                Assert.Equal("culture=michigan", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public void SingleQuoteSaveNewLine()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.Write("culture", "michigan", ValueWrite.Quoted);
                }
                Assert.Equal("culture=\"michigan\"", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public void SaveComment()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.WriteComment("This is a comment");
                }
                Assert.Equal("#This is a comment\n", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Fact]
        public void SaveDate()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (ParadoxSaver saver = new ParadoxSaver(ms))
                {
                    saver.WriteLine("date", new DateTime(1, 1, 1));
                }
                Assert.Equal("date=\"1.1.1\"\n", Globals.ParadoxEncoding.GetString(ms.ToArray()));
            }
        }

        [Fact]
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
                Assert.Equal("name=šžŸ", actual);
            }
        }

        [Fact]
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
                Assert.Equal("0.235", actual);
            }
        }

        [Fact]
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
                Assert.Equal("-1235346", actual);
            }
        }

        [Fact]
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
                Assert.Equal("1402.1.1", actual);
            }
        }

        [Fact]
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
                Assert.Equal("{\n\tHey there!\n}\n", actual);
            }
        }
    }
}
