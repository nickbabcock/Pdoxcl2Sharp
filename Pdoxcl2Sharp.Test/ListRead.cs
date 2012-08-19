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
        IEnumerable<double> actualFloat = null;

        Action<ParadoxParser, string> action;
        Action<ParadoxParser, string> floatAction;

        int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };
        double[] expectedFloat = { 1.200, 0.63, 2.11, 23.421 };
        [TestFixtureSetUp]
        public void Setup()
        {
            action = (parser, token) =>
            {
                if (token == "list")
                    actual = parser.ReadIntList();
            };

            floatAction = (parser, token) =>
                {
                    if (token == "list")
                        actualFloat = parser.ReadFloatList();
                };
        }

        [SetUp]
        public void Nullify()
        {
            actual = null;
            actualFloat = null;
        }

        [Test]
        public void SimpleList()
        {
            byte[] data = "list={1 2 3 4 5 6 7 8}".ToByteArray();

            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void SpaceList()
        {
            var data = "list = { 1 2 3 4 5 6 7 8 }".ToByteArray();
            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void VerySpacedList()
        {
            var data = "   list    =   {   1 2  3  4 5 6  7    8    }    ".ToByteArray();
            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void SimpleFloatingList()
        {
            byte[] data = "list={1.200 .63 2.11 23.421}".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(expectedFloat, actualFloat);
        }

        [Test]
        public void SpacelFloatingList()
        {
            byte[] data = "list = { 1.200 .63 2.11 23.421 }".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(expectedFloat, actualFloat);
        }

        [Test]
        public void VerySpacedFloatingList()
        {
            byte[] data = "   list   =  { 1.200    .63 2.11  23.421   }    ".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(expectedFloat, actualFloat);
        }

        [Test]
        public void FloatPrecedingAndTrailingZeroesDontMatter()
        {
            byte[] data = "list={ 0001.200 0.63 2.110000 0023.421000 }".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(expectedFloat, actualFloat);
        }

        [Test]
        public void IntEmptyList()
        {
            var data = "list={}".ToByteArray();
            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEqual(Enumerable.Empty<int>(), actual);
        }

        [Test]
        public void IntEmptySpacedList()
        {
            var data = "  list  =   {    }    ".ToByteArray();
            ParadoxParser.Parse(data, action);
            CollectionAssert.AreEqual(Enumerable.Empty<int>(), actual);
        }


        [Test]
        public void DoubleEmptySpacedList()
        {
            var data = "     list   =     {    }   ".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(Enumerable.Empty<double>(), actualFloat);
        }

        [Test]
        public void DoubleEmptyList()
        {
            var data = "list{}".ToByteArray();
            ParadoxParser.Parse(data, floatAction);
            CollectionAssert.AreEqual(Enumerable.Empty<double>(), actualFloat);
        }

        [Test]
        public void SimpleReadStringList()
        {
            var data = "\tinfantry_brigade = {\r\n\t\"III División 'Pellegrini'\" \"II División 'San Martín'\" \r\n \"I División 'Ing. Krausse'\"}".ToByteArray();
            string[] expected = { "III División 'Pellegrini'", "II División 'San Martín'", "I División 'Ing. Krausse'" };
            Test<string>(data, x => x.ReadStringList(), expected, "infantry_brigade");
        }

        [Test]
        public void ReadStringListEmpty()
        {
            var data = "infantry_brigade = { }".ToByteArray();
            Test<string>(data, x => x.ReadStringList(), Enumerable.Empty<string>(), "infantry_brigade");
        }

        [Test]
        public void ReadStringListEmptyNoSpace()
        {
            var data = "infantry_brigade={}".ToByteArray();
            Test<string>(data, x => x.ReadStringList(), Enumerable.Empty<string>(), "infantry_brigade");
        }

        [Test]
        public void ReadTechnologyStringList()
        {
            var data = @"theoretical= {
	infantry_theory
	militia_theory
	mobile_theory
}".ToByteArray();
            string[] expected = {"infantry_theory", "militia_theory", "mobile_theory"};
            Test<string>(data, x => x.ReadStringList(), expected, "theoretical");
        }


        private void Test<T>(byte[] data, Func<ParadoxParser, IEnumerable<T>> func, IEnumerable<T> expected, string tokenStr)
        {
            IEnumerable<T> actual = null;

            Action<ParadoxParser, string> act = (parser, token) =>
                {
                    if (token == tokenStr)
                        actual = func(parser);
                };

            ParadoxParser.Parse(data, act);
            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
