using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp
{
    class TextHelpers
    {
        public static bool IsWhiteSpace(byte b) =>
            b == TextConstants.Space ||
            b == TextConstants.Tab ||
            b == TextConstants.LineFeed ||
            b == TextConstants.CarriageReturn;
    }
}
