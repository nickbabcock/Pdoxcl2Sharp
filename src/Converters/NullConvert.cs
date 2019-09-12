using System;

namespace Pdoxcl2Sharp.Converters
{
    internal class NullConvert : TextConvert
    {
        public override object BaseRead(ref ParadoxTextReader reader, Type typeToConvert, ParadoxSerializerOptions options) => Activator.CreateInstance(typeToConvert);
        public override Type TypeToConvert()
        {
            throw new NotImplementedException();
        }
    }
}
