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
    }
}
