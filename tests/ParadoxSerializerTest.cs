using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Pdoxcl2Sharp.Tests
{
    public class ScratchTest
    {
        class MyData
        {
            public string Hello { get; set; }
        }

        [Theory]
        [InlineData("hello=world")]
        [InlineData("hello = world")]
        [InlineData("\t\thello =\r\n world")]
        [InlineData("#1st\r\nhello #2nd\r\n= #3rd\r\n world #4th\r\n")]
        [InlineData("hello==world")]
        [InlineData("header hello = world")]
        [InlineData("a = 1.000\r\nhello=world\r\nc=d")]
        public async void TestOneProperty(string input)
        {
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyData>(mem);
            Assert.Equal("world", res.Hello);
        }

        [Fact]
        public async void TestLongStringProperty()
        {
            var options = new ParadoxSerializerOptions {DefaultBufferSize = 10};
            var input = @"hello=""Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt""";
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyData>(mem, options);
            Assert.Equal("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt", res.Hello);
        }

        [Theory]
        [InlineData("hello=world\r\nabc=def")]
        [InlineData("abc=def\r\nhello=world")]
        [InlineData("abc=def\r\nhello=world\r\nfoo=bar")]
        public async void TestOneIgnoreProperty(string input)
        {
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyData>(mem);
            Assert.Equal("world", res.Hello);
        }

        class MyList
        {
            public List<int> Numbers { get; set; }
        }

        [Theory]
        [InlineData("numbers = { 1 2 3 }")]
        public async void TestList(string input)
        {
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyList>(mem);
            Assert.Equal(new List<int> { 1, 2, 3 }, res.Numbers);
        }

        class MyStringList
        {
            public List<string> Entries { get; set; }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(30)]
        public async void TestStringList(int? bufferSize)
        {
            var options = new ParadoxSerializerOptions();
            if (bufferSize != null)
            {
                options.DefaultBufferSize = bufferSize.Value;
            }
            var input = "entries = { hello goodbye sincere hooks code go rust see sharp }";
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyStringList>(mem, options);
            Assert.Equal(new List<string> { "hello", "goodbye", "sincere", "hooks", "code", "go", "rust", "see", "sharp" }, res.Entries);
        }

        class MyTypes
        {
            public string Hello { get; set; }
            public int MyInt { get; set; }
        }

        [Fact]
        public async void TestManyTypes()
        {
            var input = @"
hello=world
my_int = 13
";
            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyTypes>(mem);
            Assert.Equal("world", res.Hello);
            Assert.Equal(13, res.MyInt);
        }


        [Theory]
        [InlineData(null)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(30)]
        public async void TestManyTypesWithIgnore(int? bufferSize)
        {
            var input = @"
a = {
  d = {
      hello=mercury
      my_int = 12
      my_list = { 29.614 23.691 20.541 16.456 13.671 12.309 11.906 11.906 11.583 11.230 11.152 }
  }
}
hello=world
my_int = 13
b = {
  c = {
    hello=mars
    my_int=11
  }
}
";

            var options = new ParadoxSerializerOptions();
            if (bufferSize != null)
            {
                options.DefaultBufferSize = bufferSize.Value;
            }

            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyTypes>(mem, options);
            Assert.Equal("world", res.Hello);
            Assert.Equal(13, res.MyInt);
        }


        class MyNestedObject
        {
            public string Hello { get; set; }
            public MyData Data { get; set; }
            public MyTypes Types { get; set; }
        }

        [Fact]
        public async void TestNestedObject()
        {
            var input = @"
hello=world
data={ hello=mars }
types = {
    hello=venus
    my_int = 13
}
";

            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyNestedObject>(mem);
            Assert.Equal("world", res.Hello);
            Assert.Equal("mars", res.Data.Hello);
            Assert.Equal("venus", res.Types.Hello);
            Assert.Equal(13, res.Types.MyInt);
        }

        [Fact]
        public async void TestNestedObjectBrace()
        {
            var input = @"
hello=world
data={ hello=mars }
types {
    hello=venus
    my_int = 13
}
";

            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyNestedObject>(mem);
            Assert.Equal("world", res.Hello);
            Assert.Equal("mars", res.Data.Hello);
            Assert.Equal("venus", res.Types.Hello);
            Assert.Equal(13, res.Types.MyInt);
        }

        [Fact]
        public async void TestNestedObjectReverse()
        {
            var input = @"
data={ hello=mars }
types = {
    hello=venus
    my_int = 13
}
hello=world
";

            var mem = new MemoryStream(Encoding.ASCII.GetBytes(input));
            var res = await Scratch.DeserializeAsync<MyNestedObject>(mem);
            Assert.Equal("world", res.Hello);
            Assert.Equal("mars", res.Data.Hello);
            Assert.Equal("venus", res.Types.Hello);
            Assert.Equal(13, res.Types.MyInt);
        }
    }
}
