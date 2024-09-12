using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp.Test
{
    public class FileSave
    {
        private static string filePath = "FileTest.txt";
        private static string outputPath = filePath + ".out";
        private static string compressedFilePath = "FileTextCompressed.txt";
        private static string outputCompressedPath = compressedFilePath + ".out";

        private string player;
        private DateTime date;
        private InnerInfo inner;

        public FileSave()
        {
            date = new DateTime(1641, 6, 11);
            player = "ALG";
            inner = new InnerInfo()
            {
                Id = 1,
                Name = "Stocšžolm",
                DiscoveredBy = new[] { "REB", "SWE" },
                CitySize = 12.000,
                GenericInfantry = new[] { "infantry_brigade", "infantry_brigade", "infantry_brigade" }
            };
        }

        [Fact(Skip = "todo")]
        public void SaveNoChange()
        {
            FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
            using (ParadoxSaver saver = new ParadoxSaver(output))
            {
                saver.WriteLine("date", date);
                saver.WriteLine("player", player, ValueWrite.Quoted);
                saver.Write(inner.Id.ToString(), inner);
            }
            Assert.Equal(File.ReadAllText(filePath), File.ReadAllText(outputPath));
        }

        [Fact]
        public void SaveNoChangeCompressed()
        {
            FileStream output = new FileStream(outputCompressedPath, FileMode.Create, FileAccess.ReadWrite);
            using (ParadoxCompressedSaver saver = new ParadoxCompressedSaver(output))
            {
                saver.WriteLine("date", date);
                saver.WriteLine("player", player, ValueWrite.Quoted);
                saver.Write(inner.Id.ToString(), inner);
            }
            Assert.Equal(File.ReadAllText(compressedFilePath), File.ReadAllText(outputCompressedPath));
        }

        private class InnerInfo : IParadoxWrite
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<string> DiscoveredBy { get; set; }
            public double CitySize { get; set; }
            public IEnumerable<string> GenericInfantry { get; set; }

            public void Write(ParadoxStreamWriter writer)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
                writer.WriteLine("citysize", CitySize);
                writer.Write("discovered_by={", ValueWrite.LeadingTabs);
                foreach (var country in DiscoveredBy)
                {
                    writer.Write(country, ValueWrite.None);
                    writer.Write(" ", ValueWrite.None);
                }

                writer.Write("}", ValueWrite.NewLine);

                writer.WriteLine("generic_infantry = {", ValueWrite.LeadingTabs);
                foreach (var infantry in GenericInfantry)
                {
                    writer.WriteLine(infantry, ValueWrite.LeadingTabs);
                }
                writer.WriteLine("}", ValueWrite.LeadingTabs);
            }
        }
    }
}
