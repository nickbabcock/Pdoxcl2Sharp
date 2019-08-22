using System;
using System.Text;

namespace Pdoxcl2Sharp.Utils
{
    public static class TextHelpers
    {

        public static readonly Encoding Windows1252Encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        public static string Transcode(ReadOnlySpan<byte> span)
        {
            unsafe
            {
                fixed (byte* ptr = span)
                {
                    return Windows1252Encoding.GetString(ptr, span.Length);
                }
            }
        }

        public static bool IsWhiteSpace(byte b) =>
            b == TextConstants.Space ||
            b == TextConstants.Tab ||
            b == TextConstants.LineFeed ||
            b == TextConstants.CarriageReturn;

        private static readonly byte[] BoundaryCharacters = {
            TextConstants.Space,
            TextConstants.Tab,
            TextConstants.LineFeed,
            TextConstants.CarriageReturn,
            TextConstants.BraceClose,
            TextConstants.BraceOpen,
            TextConstants.Equal,
            TextConstants.Exclamation,
            TextConstants.GreaterThan,
            TextConstants.LessThan
        };

        public static int IndexOfBoundary(this ReadOnlySpan<byte> span) => span.IndexOfAny(BoundaryCharacters);
    }
}
