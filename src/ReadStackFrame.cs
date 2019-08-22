using System.Collections.Generic;
using Pdoxcl2Sharp.Parsers;

namespace Pdoxcl2Sharp
{
    internal class ReadStackFrame
    {
        public object ReturnValue;
        public Dictionary<ulong, DecodeProperty> Properties;
    }
}
