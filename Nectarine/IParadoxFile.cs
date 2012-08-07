using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nectarine
{
    public interface IParadoxFile
    {
        IDictionary<string, Action<ParadoxParser>> ParseValues { get; }
    }
}
