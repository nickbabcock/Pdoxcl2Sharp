using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    public class FileSave
    {
        private static string filePath = "FileTest.txt";
        private static string outputPath = filePath + ".out";

        [Test]
        public void SaveNoChange()
        {
            using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
            using (ParadoxSaver saver = new ParadoxSaver(output))
            {
                var date = new DateTime(1641, 6, 11);
                var player = "ALG";
                var inner = new InnerInfo()
                {
                    Id = 1,
                    Name = "Stocšžolm",
                    DiscoveredBy = new[] {"REB", "SWE"},
                    CitySize = 12.000,
                    GenericInfantry = new[] {"infantry_brigade", "infantry_brigade", "infantry_brigade"}
                };

                saver.WriteLine("date", date);
                saver.WriteLine("player", player, ValueWrite.Quoted);
                saver.Write(inner.Id.ToString(), inner);
            }
            FileAssert.AreEqual(filePath, outputPath);
        }

        private class InnerInfo : IParadoxWrite
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<string> DiscoveredBy { get; set; }
            public double CitySize { get; set; }
            public IEnumerable<string> GenericInfantry { get; set; }

            public void Write(ParadoxSaver writer)
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
