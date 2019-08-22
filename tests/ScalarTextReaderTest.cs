using System;
using Pdoxcl2Sharp.Utils;
using Xunit;

namespace Pdoxcl2Sharp.Tests
{
    public class ScalarTextReaderTest
    {
        [Theory]
        [InlineData("eu4txt\r")]
        [InlineData("\reu4txt\r")]
        [InlineData("\reu4txt")]
        [InlineData("eu4txt\r\n")]
        [InlineData("\r\neu4txt\r\n")]
        [InlineData("\r\neu4txt")]
        [InlineData("eu4txt\n")]
        [InlineData("\neu4txt\n")]
        [InlineData("\neu4txt")]
        [InlineData("eu4txt\t")]
        [InlineData("\teu4txt\t")]
        [InlineData("\teu4txt")]
        [InlineData("eu4txt ")]
        [InlineData(" eu4txt ")]
        [InlineData(" eu4txt")]
        [InlineData("eu4txt")]
        [InlineData("  \t\r\neu4txt\r\n\t   ")]
        public void TestGetString(string str)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal("eu4txt", reader.GetString());
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData(" 1 ", 1)]
        [InlineData("\r\n-1\r\n", -1)]
        [InlineData("\t0\t", 0)]
        public void TestGetInt32(string str, int expected)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal(expected, reader.GetInt32());
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("1.000", 1.0)]
        [InlineData("-1.000", -1.0)]
        [InlineData("0.000", 0.0)]
        [InlineData("1.00000", 1.0)]
        [InlineData("-1.00000", -1.0)]
        [InlineData("0.00000", 0.0)]
        [InlineData("1", 1.0)]
        [InlineData("-1", -1.0)]
        [InlineData("0", 0.0)]
        [InlineData("15.352", 15.352)]
        public void TestGetDouble(string str, double expected)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal(expected, reader.GetDouble());
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("#this is a comment", "this is a comment")]
        [InlineData("line #comment", "comment")]
        [InlineData("# comment with space ", " comment with space ")]
        [InlineData("\n#comment\r", "comment")]
        [InlineData("\r\n#comment\r\n", "comment")]
        [InlineData("\n#comment\n", "comment")]
        public void TestGetComment(string str, string expected)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            while (reader.Read() && reader.TokenType != TextTokenType.Comment)
            {
            }

            Assert.Equal(TextTokenType.Comment, reader.TokenType);
            Assert.Equal(expected, reader.GetComment());
        }

        [Theory]
        [InlineData("a=", OperatorType.Equal)]
        [InlineData("a = ", OperatorType.Equal)]
        [InlineData("a >", OperatorType.Greater)]
        [InlineData("a >=", OperatorType.GreaterEqual)]
        [InlineData("a <=", OperatorType.LesserEqual)]
        [InlineData("a <", OperatorType.Lesser)]
        [InlineData("a <>", OperatorType.LesserGreater)]
        [InlineData("a !=", OperatorType.NotEqual)]
        public void TestGetOperator(string str, OperatorType op)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal("a", reader.GetString());
            Assert.True(reader.Read());
            Assert.Equal(op, reader.GetOperator());
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("\"eu4txt\"", "eu4txt")]
        [InlineData("  \"{ eu4txt }\""  , "{ eu4txt }")]
        [InlineData("  \"abc\r\ndef\""  , "abc\r\ndef")]
        public void TestGetQuotes(string str, string expected)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal(expected, reader.GetString());
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("1444.10.09", 1444, 10, 9)]
        [InlineData("1840.1.1", 1840, 1, 1)]
        [InlineData("1.1.1", 1, 1, 1)]
        public void TestGetDate(string str, int year, int month, int day)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);
            Assert.True(reader.Read());
            Assert.Equal(new DateTime(year, month, day), reader.GetDate());
            Assert.False(reader.Read());
        }
    }
}
