using Pdoxcl2Sharp.Naming;
using Pdoxcl2Sharp.Parsers;
using Xunit;

namespace Pdoxcl2Sharp.Tests
{
    public class NamingConventionTest
    {
        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("Hello", "hello")]
        [InlineData("HelloWorld", "hello_world")]
        public void TestSnakeNaming(string input, string expected)
        {
            var naming = new SnakeNaming();
            Assert.Equal(expected, naming.ConvertName(input));
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("Hello", "Hello")]
        [InlineData("HelloWorld", "HelloWorld")]
        public void TestNoopNaming(string input, string expected)
        {
            var naming = new NoopNaming();
            Assert.Equal(expected, naming.ConvertName(input));
        }
    }
}
