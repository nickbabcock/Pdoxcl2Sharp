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

        public static int IndexOfBoundary(this ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                switch (span[i])
                {
                    case TextConstants.Space:
                    case TextConstants.Tab:
                    case TextConstants.LineFeed:
                    case TextConstants.CarriageReturn:
                    case TextConstants.BraceClose:
                    case TextConstants.BraceOpen:
                    case TextConstants.Equal:
                    case TextConstants.Exclamation:
                    case TextConstants.GreaterThan:
                    case TextConstants.LessThan:
                        return i;
                }
            }

            return -1;
        }
    }
}
