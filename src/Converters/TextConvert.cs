using System;

namespace Pdoxcl2Sharp.Converters
{
    public abstract class TextConvert
    {
        public abstract object BaseRead(ref ParadoxTextReader reader, Type typeToConvert, ParadoxSerializerOptions options);
    }

    public abstract class TextConvert<T> : TextConvert
    {
        public override object BaseRead(ref ParadoxTextReader reader, Type typeToConvert,
            ParadoxSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public abstract T Read(ref ParadoxTextReader reader, Type typeToConvert, ParadoxSerializerOptions options);
    }
}
