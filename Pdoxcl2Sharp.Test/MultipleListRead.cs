using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Pdoxcl2Sharp.Test
{
    [TestFixture]
    class MultipleListRead
    {
        [Test]
        public void SimpleMultiple()
        {
            var data = "list = { 1 2 3 4 } \r\n list2 = { 1 2 3 4 }".ToByteArray();
            Test<int>(data, x => x.ReadIntList(), new[] { 1, 2, 3, 4 }, "list", "list2");
        }

        private void Test<T>(byte[] data,
                             Func<ParadoxParser, IEnumerable<T>> func,
                             IEnumerable<T> expected,
                             string token1,
                             string token2)
        {
            IEnumerable<T> actual1 = null;
            IEnumerable<T> actual2 = null;;
            Action<ParadoxParser, string> action = (p, token) =>
                {
                    if (token == token1)
                         actual1 = func(p);
                    else if (token == token2)
                        actual2 = func(p);
                };
            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEquivalent(expected, actual1);
            CollectionAssert.AreEquivalent(expected, actual2);
        }
    }
}
