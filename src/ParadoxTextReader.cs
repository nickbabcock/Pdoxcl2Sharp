using System;

namespace Pdoxcl2Sharp
{
    public ref partial struct ParadoxTextReader
    {
        private int _consumed;
        private ReadOnlySpan<byte> _buffer;
        private readonly bool _isFinalBlock;
        private int _bytePositionInLine;
        private int _lineNumber;

        public ParadoxTextReader(ReadOnlySpan<byte> data, bool isFinalBlock, TextReaderState state)
        {
            _buffer = data;
            _isFinalBlock = isFinalBlock;
            _consumed = 0;
            TokenType = state._tokenType;
            _bytePositionInLine = 0;
            _lineNumber = 0;
            TokenStartIndex = 0;
            ValueSpan = ReadOnlySpan<byte>.Empty;
        }

        public ReadOnlySpan<byte> ValueSpan { get; private set; }

        public TextTokenType TokenType { get; private set; }
        public int TokenStartIndex { get; private set; }

        public bool Read()
        {
            if (!HasMoreData())
            {
                return false;
            }

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
                case TextConstants.BraceOpen:
                    _consumed++;
                    _bytePositionInLine++;
                    TokenType = TextTokenType.Open;
                    break;
                case TextConstants.BraceClose:
                    _consumed++;
                    _bytePositionInLine++;
                    TokenType = TextTokenType.End;
                    break;
                case TextConstants.Equal:
                case TextConstants.Exclamation:
                case TextConstants.LessThan:
                case TextConstants.GreaterThan:
                    TokenType = TextTokenType.Operator;
                    return ConsumeOperator();
                case TextConstants.Comment:
                    TokenType = TextTokenType.Comment;
                    return ConsumeComment();
                case TextConstants.Quote:
                    TokenType = TextTokenType.Scalar;
                    return ConsumeQuote();
                default:
                    TokenType = TextTokenType.Scalar;
                    return ConsumeScalar();
            }

            return true;
        }

        private bool HasMoreData()
        {
            return _consumed < (uint) _buffer.Length;
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

        private bool ConsumeScalar()
        {
            var localBuffer = _buffer.Slice(_consumed);
            var idx = localBuffer.IndexOfBoundary();
            if (idx >= 0)
            {
                ValueSpan = localBuffer.Slice(0, idx);
                _bytePositionInLine += idx;
                _consumed += idx;
                return true;
            }

            if (!_isFinalBlock)
            {
                return false;
            }

            ValueSpan = localBuffer;
            _bytePositionInLine += localBuffer.Length;
            _consumed += localBuffer.Length;
            return true;
        }

        private bool ConsumeQuote()
        {
            var localBuffer = _buffer.Slice(_consumed + 1);
            int idx = localBuffer.IndexOf(TextConstants.Quote);
            if (idx >= 0)
            {
                ValueSpan = localBuffer.Slice(0, idx);
                _consumed += idx + 2;

                var span = ValueSpan;
                int lnIdx = span.LastIndexOf(TextConstants.LineFeed);
                if (lnIdx >= 0)
                {
                    _bytePositionInLine += span.Length - lnIdx + 1;
                }
                else
                {
                    _bytePositionInLine += idx + 2;
                }

                return true;
            }

            if (_isFinalBlock)
            {
                throw new InvalidOperationException("missing end quote");
            }

            return false;
        }


        private bool ConsumeComment()
        {
            var localBuffer = _buffer.Slice(_consumed);
            var idx = localBuffer.IndexOfAny(TextConstants.CarriageReturn, TextConstants.LineFeed);
            if (idx >= 0)
            {
                ValueSpan = localBuffer.Slice(1, idx - 1);
                _bytePositionInLine += idx;
                _consumed += idx;
                return true;
            }

            if (!_isFinalBlock)
            {
                return false;
            }

            ValueSpan = localBuffer.Slice(1);
            _bytePositionInLine += localBuffer.Length;
            _consumed += localBuffer.Length;
            return true;
        }

        private bool ConsumeOperator()
        {
            var localBuffer = _buffer.Slice(_consumed);
            byte first = localBuffer[0];

            // The only operators that begins with an equals is the equal operator
            if (first == TextConstants.Equal)
            {
                ValueSpan = localBuffer.Slice(0, 1);
                _bytePositionInLine += 1;
                _consumed += 1;
            }
            else if (localBuffer.Length >= 2)
            {
                var next = localBuffer[1];

                // Only operators that have a length of two are '>=, <=, !=, and <>'
                var len = next == TextConstants.Equal || next == TextConstants.GreaterThan ? 2 : 1;
                ValueSpan = localBuffer.Slice(0, len);
                _bytePositionInLine += len;
                _consumed += len;
            }
            else
            {
                // The only operators with a single letter (other than equals) are '< and >'
                if (!((first == TextConstants.LessThan || first == TextConstants.GreaterThan) && _isFinalBlock))
                {
                    return false;
                }

                ValueSpan = localBuffer.Slice(0, 1);
                _bytePositionInLine += 1;
                _consumed += 1;
            }

            return true;
        }
    }
}
