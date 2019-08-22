using System;
using System.Collections.Generic;
using System.Text;

namespace Pdoxcl2Sharp.Parsers
{
    internal interface IParse<out T>
    {
        void Parse(ref ParadoxTextReader reader);

        T Result { get; }
    }
}
