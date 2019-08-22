
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pdoxcl2Sharp.Parsers
{
    internal class TextObjectParser : IParse<object>
    {
        private enum ObjectState
        {
            Property,
            Operator,
            Value
        }

        public enum PropertyType
        {
            None,
            Scalar,
            Object,
            Array
        }

          private readonly ReadStack _stack;
        private PropertyType _found = PropertyType.None;
        private ObjectState _state = ObjectState.Property;

        internal TextObjectParser(ReadStack stack)
        {
            _stack = stack;
        }

        public void Parse(ref ParadoxTextReader reader)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case TextTokenType.Scalar when _state == ObjectState.Property:
                    {
                        _found = _stack.FoundProperty(reader.ValueSpan);
                        if (_found != PropertyType.None)
                        {
                            _state = ObjectState.Operator;
                        }
                        break;
                    }

                    case TextTokenType.Scalar when _state == ObjectState.Value && _found == PropertyType.Scalar:
                    {
                        _state = ObjectState.Property;
                        _stack.FoundValue(ref reader);
                        break;
                    }
                    case TextTokenType.Comment:
                        continue;
                    case TextTokenType.Operator:
                        if (_state == ObjectState.Operator)
                        {
                            _state = ObjectState.Value;
                        }
                        break;
                    case TextTokenType.Open when _state == ObjectState.Value && _found == PropertyType.Object:
                        _stack.Push();
                        _state = ObjectState.Property;
                        break;
                    case TextTokenType.End when _state == ObjectState.Property:
                        _stack.Pop();
                        break;
                    default:
                        break;
                }
            }
        }

        public object Result => _stack.ReturnValue;
    }
}
