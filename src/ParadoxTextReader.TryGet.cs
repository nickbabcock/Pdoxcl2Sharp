using System;
using System.Buffers.Text;
using Pdoxcl2Sharp.Utils;

namespace Pdoxcl2Sharp
{
    public ref partial struct ParadoxTextReader
    {
        private void EnsureScalar()
        {
            if (TokenType != TextTokenType.Scalar)
            {
                throw new InvalidOperationException($"Expected scalar but received: {TokenType}");
            }
        }

        public string GetString()
        {
            EnsureScalar();
            return TextHelpers.Transcode(ValueSpan);
        }

        public int GetInt32()
        {
            if (!TryGetInt32(out int value))
            {
                throw new InvalidOperationException($"Expected number but received: {GetString()}");
            }

            return value;
        }

        public bool TryGetInt32(out int value)
        {
            EnsureScalar();
            var span = ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && bytesConsumed == span.Length;
        }

        public double GetDouble()
        {
            if (!TryGetDouble(out double value))
            {
                throw new InvalidOperationException($"Expected number but received: {GetString()}");
            }

            return value;
        }

        public bool TryGetDouble(out double value)
        {
            EnsureScalar();
            var span = ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && bytesConsumed == span.Length;
        }

        public string GetComment()
        {
            if (TokenType != TextTokenType.Comment)
            {
                throw new InvalidOperationException($"Expected comment but received: {TokenType}");
            }

            return TextHelpers.Transcode(ValueSpan);
        }

        public DateTime GetDate()
        {
            EnsureScalar();
            var span = ValueSpan;
            int yIdx = span.IndexOf(TextConstants.Period);
            if (yIdx == -1 || !Utf8Parser.TryParse(span.Slice(0, yIdx), out int year, out int bytes) || bytes != yIdx)
            {
                throw new InvalidOperationException("Need period delimiter for year");
            }

            span = span.Slice(yIdx + 1);
            int mIdx = span.IndexOf(TextConstants.Period);
            if (mIdx == -1 || !Utf8Parser.TryParse(span.Slice(0, mIdx), out int month, out bytes) || bytes != mIdx)
            {
                throw new InvalidOperationException("Need period delimiter for month");
            }

            span = span.Slice(mIdx + 1);
            int dIdx = span.IndexOf(TextConstants.Period);
            if (dIdx == -1)
            {
                if (!Utf8Parser.TryParse(span, out int day, out bytes) || bytes != span.Length)
                {
                    throw new InvalidOperationException("Unrecognized day for datetime");
                }
                return new DateTime(year, month, day);
            }

            throw new NotImplementedException();
        }

        public OperatorType GetOperator()
        {
            if (TokenType != TextTokenType.Operator)
            {
                throw new InvalidOperationException($"Expected operator but received: {TokenType}");
            }

            var span = ValueSpan;
            if (span.Length == 1)
            {
                switch (span[0])
                {
                    case TextConstants.Equal:
                        return OperatorType.Equal;
                    case TextConstants.LessThan:
                        return OperatorType.Lesser;
                    case TextConstants.GreaterThan:
                        return OperatorType.Greater;
                    default:
                        throw new InvalidOperationException($"unexpected operator {(char)span[0]}");
                }
            }

            byte first = span[0];
            byte next = span[1];

            if (first == TextConstants.LessThan && next == TextConstants.Equal)
            {
                return OperatorType.LesserEqual;
            }

            if (first == TextConstants.GreaterThan && next == TextConstants.Equal)
            {
                return OperatorType.GreaterEqual;
            }

            if (first == TextConstants.LessThan && next == TextConstants.GreaterThan)
            {
                return OperatorType.LesserGreater;
            }


            if (first == TextConstants.Exclamation && next == TextConstants.Equal)
            {
                return OperatorType.NotEqual;
            }

            throw new InvalidOperationException($"unexpected operator: {(char)first} {(char)next}");
        }
    }
}
