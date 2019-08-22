using System;
using Pdoxcl2Sharp.Utils;
using Xunit;

namespace Pdoxcl2Sharp.Tests
{
    public partial class TextReaderTest
    {
        public enum ParaValue : byte
        {
            Int,
            String,
            Operator,
            Open,
            End
        }

        [Theory]
        [InlineData("abc", ParaValue.String, "abc")]
        [InlineData("foo=",
            ParaValue.String, "foo",
            ParaValue.Operator, OperatorType.Equal
        )]
        [InlineData("foo=10",
            ParaValue.String, "foo",
            ParaValue.Operator, OperatorType.Equal,
            ParaValue.Int, 10
        )]
        [InlineData("eu4txt\nfoo=10",
            ParaValue.String, "eu4txt",
            ParaValue.String, "foo",
            ParaValue.Operator, OperatorType.Equal,
            ParaValue.Int, 10
        )]
        [InlineData("eu4txt\nabc={name=foo}",
            ParaValue.String, "eu4txt",
            ParaValue.String, "abc",
            ParaValue.Operator, OperatorType.Equal,
            ParaValue.Open, "{",
            ParaValue.String, "name",
            ParaValue.Operator, OperatorType.Equal,
            ParaValue.String, "foo",
            ParaValue.End, "}"
        )]
        [InlineData("arr={abc def c d}",
            ParaValue.String, "arr",
            ParaValue.Operator, OperatorType.Equal,
            ParaValue.Open, "{",
            ParaValue.String, "abc",
            ParaValue.String, "def",
            ParaValue.String, "c",
            ParaValue.String, "d",
            ParaValue.End, "}"
        )]
        public void TextReaderParseOrder(string str, params object[] input)
        {
            var data = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(str));
            var reader = new ParadoxTextReader(data, isFinalBlock: true, state: default);

            for (int i = 0; i < input.Length; i+=2)
            {
                Assert.True(reader.Read());
                Assert.Equal(GetToken((ParaValue) input[i]), reader.TokenType);
                Assert.Equal(GetValue(ref reader, (ParaValue) input[i]), input[i + 1]);

            }

            Assert.False(reader.Read());
        }

        public TextTokenType GetToken(ParaValue value)
        {
            switch (value)
            {
                case ParaValue.Int:
                case ParaValue.String:
                    return TextTokenType.Scalar;
                case ParaValue.Operator:
                    return TextTokenType.Operator;
                case ParaValue.Open:
                    return TextTokenType.Open;
                case ParaValue.End:
                    return TextTokenType.End;
                default:
                    throw new ArgumentException("unexpected value");
            }
        }

        public object GetValue(ref ParadoxTextReader reader, ParaValue value)
        {
            switch (value)
            {
                case ParaValue.String:
                    return reader.GetString();
                case ParaValue.Operator:
                    return reader.GetOperator();
                case ParaValue.Int:
                    return reader.GetInt32();
                case ParaValue.Open:
                    return "{";
                case ParaValue.End:
                    return "}";
                default:
                    throw new ArgumentException("unexpected value");
            }
        }
    }
}
