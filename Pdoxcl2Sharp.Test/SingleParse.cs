using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pdoxcl2Sharp;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class SingleParse
    {
        [Test]
        public void ReadSingleString()
        {
            var data = "michigan".ToStream();
            string actual = string.Empty;
            Action<ParadoxParser, string> action = (x, s) => actual = s;
            ParadoxParser.Parse(data, action);
            Assert.AreEqual("michigan", actual);
        }

        [Test]
        public void ReadSingleSpacedString()
        {
            var data = "   michigan    ".ToStream();
            string actual = string.Empty;
            Action<ParadoxParser, string> action = (x, s) => actual = s;
            ParadoxParser.Parse(data, action);
            Assert.AreEqual("michigan", actual);
        }
    }
}
