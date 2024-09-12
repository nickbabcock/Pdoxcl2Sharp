using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Pdoxcl2Sharp;
using System.IO;
namespace Pdoxcl2Sharp.Test
{
    public class ListRead
    {
        IEnumerable<int> actual = null;
        IEnumerable<double> actualFloat = null;

        Action<ParadoxParser, string> action;
        Action<ParadoxParser, string> floatAction;

        int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };
        double[] expectedFloat = { 1.200, 0.63, 2.11, 23.421 };

        public ListRead()
        {
            action = (parser, token) =>
            {
                if (token == "list")
                    actual = parser.ReadIntList();
            };

            floatAction = (parser, token) =>
                {
                    if (token == "list")
                        actualFloat = parser.ReadDoubleList();
                };
        }

/*        [SetUp]
        public void Nullify()
        {
            actual = null;
            actualFloat = null;
        }*/

        [Fact]
        public void SimpleList()
        {
            Stream data = "list={1 2 3 4 5 6 7 8}".ToStream();

            ParadoxParser.Parse(data, action);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SpaceList()
        {
            var data = "list = { 1 2 3 4 5 6 7 8 }".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void VerySpacedList()
        {
            var data = "   list    =   {   1 2  3  4 5 6  7    8    }    ".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SimpleFloatingList()
        {
            Stream data = "list={1.200 .63 2.11 23.421}".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void SpacelFloatingList()
        {
            Stream data = "list = { 1.200 .63 2.11 23.421 }".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void VerySpacedFloatingList()
        {
            Stream data = "   list   =  { 1.200    .63 2.11  23.421   }    ".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void FloatPrecedingAndTrailingZeroesDontMatter()
        {
            Stream data = "list={ 0001.200 0.63 2.110000 0023.421000 }".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void IntEmptyList()
        {
            var data = "list={}".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(Enumerable.Empty<int>(), actual);
        }

        [Fact]
        public void IntEmptySpacedList()
        {
            var data = "  list  =   {    }    ".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(Enumerable.Empty<int>(), actual);
        }


        [Fact]
        public void DoubleEmptySpacedList()
        {
            var data = "     list   =     {    }   ".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(Enumerable.Empty<double>(), actualFloat);
        }

        [Fact]
        public void DoubleEmptyList()
        {
            var data = "list={}".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(Enumerable.Empty<double>(), actualFloat);
        }

        [Fact]
        public void ReadCommaIntList()
        {
            var data = "list = { 1, 2, 3, 4, 5 }".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, actual);
        }

        [Fact]
        public void ReadCommaIntListNoSpace()
        {
            var data = "list={1,2,3,4,5}".ToStream();
            ParadoxParser.Parse(data, action);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, actual);
        }

        [Fact]
        public void ReadCommaDoubleList()
        {
            Stream data = "list = { 1.200, .63, 2.11, 23.421 }".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void ReadCommaDoubleNoSpaceList()
        {
            Stream data = "list={1.200,.63,2.11,23.421}".ToStream();
            ParadoxParser.Parse(data, floatAction);
            Assert.Equal(expectedFloat, actualFloat);
        }

        [Fact]
        public void SimpleReadStringList()
        {
            var data = "\tinfantry_brigade = {\r\n\t\"III División 'Pellegrini'\" \"II División 'San Martín'\" \r\n \"I División 'Ing. Krausse'\"}".ToStream();
            string[] expected = { "III División 'Pellegrini'", "II División 'San Martín'", "I División 'Ing. Krausse'" };
            Test<string>(data, x => x.ReadStringList(), expected, "infantry_brigade");
        }

        [Fact]
        public void ReadStringListEmpty()
        {
            var data = "infantry_brigade = { }".ToStream();
            Test<string>(data, x => x.ReadStringList(), Enumerable.Empty<string>(), "infantry_brigade");
        }

        [Fact]
        public void ReadStringListEmptyNoSpace()
        {
            var data = "infantry_brigade={}".ToStream();
            Test<string>(data, x => x.ReadStringList(), Enumerable.Empty<string>(), "infantry_brigade");
        }

        [Fact]
        public void ReadTechnologyStringList()
        {
            var data = @"theoretical= {
	infantry_theory
	militia_theory
	mobile_theory
}".ToStream();
            string[] expected = {"infantry_theory", "militia_theory", "mobile_theory"};
            Test<string>(data, x => x.ReadStringList(), expected, "theoretical");
        }

        [Fact]
        public void ReadStringCommaList()
        {
            var data = "list = { \"I'm space\", first, second }".ToStream();
            Test<string>(data, x => x.ReadStringList(), new[] { "I'm space", "first", "second" }, "list");
        }

        [Fact]
        public void ReadStringCommaNoSpaceList()
        {
            var data = "list={\"I'm space\",first,second}".ToStream();
            Test<string>(data, x => x.ReadStringList(), new[] { "I'm space", "first", "second" }, "list");
        }

        private void Test<T>(Stream data, Func<ParadoxParser, IEnumerable<T>> func, IEnumerable<T> expected, string tokenStr)
        {
            IEnumerable<T> actual = null;

            Action<ParadoxParser, string> act = (parser, token) =>
                {
                    if (token == tokenStr)
                        actual = func(parser);
                };

            ParadoxParser.Parse(data, act);
            Assert.Equal(expected, actual);
        }
    }
}
