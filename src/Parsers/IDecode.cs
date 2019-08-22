using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp.Parsers
{
    interface IDecode
    {
        void Decode(ref ParadoxTextReader reader, object obj);
    }
}
