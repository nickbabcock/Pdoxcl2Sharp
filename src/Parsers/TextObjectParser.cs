using System.Runtime.InteropServices;

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

        private readonly ReadStack _stack;
        private PropertyType _found = PropertyType.None;
        private ObjectState _state = ObjectState.Property;
        private int skipDepth;
        private int pushDepth;
        private int depth;

        internal TextObjectParser(ReadStack stack)
        {
            _stack = stack;
        }

        public void Parse(ref ParadoxTextReader reader)
        {
            while (reader.Read())
            {
                if (skipDepth > 0)
                {
                    switch (reader.TokenType)
                    {
                        case TextTokenType.Open:
                            skipDepth++;
                            break;
                        case TextTokenType.End:
                            skipDepth--;
                            break;
                    }

                    continue;
                }

                switch (reader.TokenType)
                {
                    case TextTokenType.Scalar when _state == ObjectState.Property:
                    {
                        _found = _stack.FoundProperty(reader.ValueSpan);
                        switch (_found)
                        {
                            case PropertyType.Object:
                            case PropertyType.Array:
                                depth++;
                                _state = ObjectState.Operator;
                                break;
                            case PropertyType.Scalar:
                                _state = ObjectState.Operator;
                                break;
                        }
                        break;
                    }

                    case TextTokenType.Scalar when _state == ObjectState.Value:
                    {
                        switch (_found)
                        {
                            case PropertyType.Scalar:
                                _state = ObjectState.Property;
                                _stack.FoundValue(ref reader);
                                break;
                            case PropertyType.Array when depth == pushDepth:
                                _stack.FoundValue(ref reader);
                                _state = ObjectState.Value;
                                break;
                            case PropertyType.Array:
                                _stack.FoundValue(ref reader);
                                _state = ObjectState.Property;
                                _stack.Pop();
                                depth--;
                                break;
                        }

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
                    case TextTokenType.Open:
                        switch (_found)
                        {
                            case PropertyType.Object:
                                pushDepth++;
                                _stack.Push();
                                _state = ObjectState.Property;
                                break;
                            case PropertyType.Array:
                                pushDepth++;
                                _stack.Push();
                                _state = ObjectState.Value;
                                break;
                            case PropertyType.None:
                                skipDepth++;
                                break;
                        }
                        break;
                    case TextTokenType.End:
                        depth--;  
                        pushDepth--;
                        _stack.Pop();
                        break;
                }
            }
        }

        public object Result => _stack.ReturnValue;
    }
}
