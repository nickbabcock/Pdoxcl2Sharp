using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp
{
    public ref partial struct ParadoxTextReader
    {
        private int _consumed;
        private ReadOnlySpan<byte> _buffer;
        private int _bytePositionInLine;
        private int _lineNumber;

        public ParadoxTextReader(ReadOnlySpan<byte> data, bool isFinalBlock, TextReaderState state)
        {
            _buffer = data;
            _consumed = 0;
            TokenType = state._tokenType;
            _bytePositionInLine = 0;
            _lineNumber = 0;
            TokenStartIndex = 0;
        }

        public TextTokenType TokenType { get; private set; }
        public int TokenStartIndex { get; private set; }

        public bool Read()
        {
            byte first = _buffer[_consumed];
            if (TextHelpers.IsWhiteSpace(first))
            {
                SkipWhiteSpace();

                if (!HasMoreData())
                {
                    return false;
                }

                first = _buffer[_consumed];
            }

            TokenStartIndex = _consumed;

            switch (first)
            {
                case TextConstants.BraceClose:
                case TextConstants.BraceOpen:
                    break;
                default:

                    break;
            }

            return true;
        }

        private bool HasMoreData()
        {
            return _consumed >= (uint) _buffer.Length;
        }

        private void SkipWhiteSpace()
        {
            // Create local copy to avoid bounds checks.
            for (var localBuffer = _buffer; _consumed < localBuffer.Length; _consumed++)
            {
                byte val = localBuffer[_consumed];
                switch (val)
                {
                    case TextConstants.CarriageReturn:
                    case TextConstants.Tab:
                    case TextConstants.Space:
                        _bytePositionInLine++;
                        break;
                    case TextConstants.LineFeed:
                        _lineNumber++;
                        _bytePositionInLine = 0;
                        break;
                    default:
                        _bytePositionInLine++;
                        return;
                }
            }
        }

        private bool ConsumeString()
        {
            var localBuffer = _buffer.Slice(_consumed);
            localBuffer.IndexOfBoundary();


            return true;
        }
    }
}
