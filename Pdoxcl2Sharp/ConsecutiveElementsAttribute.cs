using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConsecutiveElementsAttribute : Attribute
    {
    }
}
