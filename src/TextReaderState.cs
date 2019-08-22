using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp
{
    public struct TextReaderState
    {
        internal int _bytePositionInLine;
        internal int _lineNumber;
        internal TextTokenType _tokenType;
    }
}
