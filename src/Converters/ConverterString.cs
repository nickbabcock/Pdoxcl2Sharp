using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp.Converters
{
    internal sealed class ConverterString : TextConvert<string>
    {
        public override string Read(ref ParadoxTextReader reader, Type typeToConvert, ParadoxSerializerOptions options) =>
            reader.GetString();
    }
}
