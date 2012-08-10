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

        [Test]
        public void SimpleList()
        {
            var data = "list={1 2 3 4 5 6 7 8}".ToByteArray();

            IEnumerable<int> actual = null;
            Action<ParadoxParser, string> action = (parser, token) =>
                {
                    if (token == "list")
                        actual = parser.ReadIntList();
                };
            
            ParadoxParser.Parse(data, action);
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };
            CollectionAssert.AreEqual(expected, actual);

        }

    }
}
