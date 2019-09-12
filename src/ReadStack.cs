using System;
using System.Collections.Generic;
using Pdoxcl2Sharp.Parsers;

namespace Pdoxcl2Sharp
{
    internal class ReadStack
    {
        private readonly ParadoxSerializerOptions _options;
        private readonly Stack<ReadStackFrame> Frames;
        private ReadStackFrame Frame;
        private DecodeProperty Property;
        private bool popTwice;

        public ReadStack(ParadoxSerializerOptions options, Type root)
        {
            _options = options;

            Frame = new ReadStackFrame
            {
                ReturnValue = Activator.CreateInstance(root),
                Properties = ParadoxSerializer.GetOrAddClass(root, _options)
            };

            Frames = new Stack<ReadStackFrame>();
            Frames.Push(Frame);
        }

        public PropertyType FoundProperty(in ReadOnlySpan<byte> property)
        {
            var hash = Farmhash.Sharp.Farmhash.Hash64(property);
            var found = Frame.Properties.TryGetValue(hash, out Property);
            if (!found)
            {
                return PropertyType.None;
            }

            switch (Property.Type)
            {
                case PropertyType.Object:
                case PropertyType.Array:
                    var current = Property.Property.GetValue(Frame.ReturnValue);
                    if (current == null)
                    {
                        var newObj = Activator.CreateInstance(Property.Property.PropertyType);
                        Property.Property.SetValue(Frame.ReturnValue, newObj);
                        current = newObj;
                    }

                    Frame = new ReadStackFrame
                    {
                        ReturnValue = current,
                        Properties = ParadoxSerializer.GetOrAddClass(Property.Property.PropertyType, _options)
                    };

                    Frames.Push(Frame);

                    if (Property.Type == PropertyType.Array && Property.ChildFormat != PropertyType.Scalar)
                    {
                        var nestedObj = Activator.CreateInstance(Property.ChildType);
                        Property.AddChild(current, nestedObj);
                        Frame = new ReadStackFrame
                        {
                            ReturnValue = nestedObj,
                            Properties = ParadoxSerializer.GetOrAddClass(Property.ChildType, _options)
                        };
                        Frames.Push(Frame);
                        popTwice = true;
                        return PropertyType.Object;
                    }

                    break;
            }

            return Property.Type;
        }

        public void FoundValue(ref ParadoxTextReader reader)
        {
            Property.Decode(ref reader, Frame.ReturnValue, _options);
        }

        public void Push()
        {
        }

        public void Pop()
        {
            Frames.Pop();
            if (popTwice)
            {
                Frames.Pop();
            }
            Frame = Frames.Peek();
        }

        public object ReturnValue => Frame.ReturnValue;
    }
}  
