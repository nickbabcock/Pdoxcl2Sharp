using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;

namespace Pdoxcl2Sharp.Test
{
    class MultipleListRead
    {
        [Fact]
        public void SimpleMultiple()
        {
            var data = "list = { 1 2 3 4 } \r\n list2 = { 1 2 3 4 }".ToStream();
            Test<int>(data, x => x.ReadIntList(), new[] { 1, 2, 3, 4 }, "list", "list2");
        }

        [Fact]
        public void NoSpaceMultiple()
        {
            var data = "list={1 2 3 4} list2={1 2 3 4}".ToStream();
            Test<int>(data, x => x.ReadIntList(), new[] { 1, 2, 3, 4 }, "list", "list2");
        }

        [Fact]
        public void SimpleDoubleMultiple()
        {
            var data = "list = { 0.091815 0.000000 0.908185 } \r\n list2 = { 0.091815 0.000000 0.908185 }".ToStream();
            Test<double>(data, x => x.ReadDoubleList(), new[] {0.091815, 0, 0.908185}, "list", "list2"); 
        }

        [Fact]
        public void NoSpaceDoubleMultiple()
        {
            var data = "list={0.091815 0.000000 0.908185} list2={0.091815 0.000000 0.908185}".ToStream();
            Test<double>(data, x => x.ReadDoubleList(), new[] { 0.091815, 0, 0.908185 }, "list", "list2"); 
        }

        private void Test<T>(Stream data,
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
            Assert.Equal(expected, actual1);
            Assert.Equal(expected, actual2);
        }
    }
}
