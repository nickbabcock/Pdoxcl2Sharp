using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp
{
    static class TextHelpers
    {
        public static bool IsWhiteSpace(byte b) =>
            b == TextConstants.Space ||
            b == TextConstants.Tab ||
            b == TextConstants.LineFeed ||
            b == TextConstants.CarriageReturn;

        private static byte[] boundaryCharacters = {
            TextConstants.Space,
            TextConstants.Tab,
            TextConstants.LineFeed,
            TextConstants.CarriageReturn,
            TextConstants.BraceClose,
            TextConstants.BraceOpen
        };

        public static int IndexOfBoundary(this ReadOnlySpan<byte> span)
        {
            return span.IndexOfAny(boundaryCharacters);
        }
    }
}
