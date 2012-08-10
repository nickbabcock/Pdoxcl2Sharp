using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pdoxcl2Sharp;
namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class ListRead
    {
        IEnumerable<int> actual = null;
        Action<ParadoxParser, string> action;

        [TestFixtureSetUp]
        public void Setup()
        {
            action = (parser, token) =>
            {
                if (token == "list")
                    actual = parser.ReadIntList();
            };
        }
        [Test]
        public void SimpleList()
        {
            byte[] data = "list={1 2 3 4 5 6 7 8}".ToByteArray();
            
            ParadoxParser.Parse(data, action);
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
