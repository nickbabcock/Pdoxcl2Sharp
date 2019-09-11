using System;
using Pdoxcl2Sharp.Utils;
using Xunit;

namespace Pdoxcl2Sharp.Tests
{
    public partial class TextReaderTest
    {
        [Fact]
        public void SingleScalarTest()
        {
            var expected = "hello";
            var slice = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(expected));
            var state = new TextReaderState();
            for (int i = 0; i < 6; i++)
            {
                var reader = new ParadoxTextReader(slice.Slice(0, i), isFinalBlock: false, state);
                Assert.False(reader.Read());

                state = reader.State;
                Assert.Equal(0, reader.Consumed);
            }

            var rdr = new ParadoxTextReader(slice.Slice(0, 5), isFinalBlock: true, state);
            Assert.True(rdr.Read());
            Assert.Equal(expected, rdr.GetString());
            Assert.Equal(5, rdr.Consumed);
        }

        [Fact]
        public void NonFinalSegmentTest()
        {
            var input = "entries = { hello goodbye sincer";
            var slice = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes(input));
            var state = new TextReaderState();
            var reader = new ParadoxTextReader(slice, isFinalBlock: false, state);
            while (reader.Read())
            {

            }
            Assert.Equal("goodbye", reader.GetString());
            Assert.Equal(26, reader.Consumed);

            slice = new ReadOnlySpan<byte>(TextHelpers.Windows1252Encoding.GetBytes("sincere }"));
            reader = new ParadoxTextReader(slice, isFinalBlock: true, reader.State);
            while (reader.Read())
            {

            }

            Assert.Equal(TextTokenType.End, reader.TokenType);
            Assert.Equal(9, reader.Consumed);
        }
    }
}
