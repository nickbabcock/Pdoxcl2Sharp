using System;

namespace Pdoxcl2Sharp.Converters
{
    internal sealed class ConverterInt32 : TextConvert<int>
    {
        public override int Read(ref ParadoxTextReader reader, Type typeToConvert, ParadoxSerializerOptions options) =>
            reader.GetInt32();
    }
}
