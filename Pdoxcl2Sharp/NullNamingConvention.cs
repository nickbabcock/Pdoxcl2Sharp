using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// No naming convention.
    /// </summary>
    public class NullNamingConvention : INamingConvention
    {
        public string Apply(string name)
        {
            return name;
        }
    }
}
