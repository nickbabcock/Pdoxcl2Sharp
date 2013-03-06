using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp.Test
{
    public static class ExtensionMethods
    {
        public static byte[] ToByteArray(this string str)
        {
            return Encoding.Default.GetBytes(str);
        }

        public static Action<ParadoxParser, string> ParserAdapter(this IDictionary<string, Action<ParadoxParser>> dictionary)
        {
            return (ParadoxParser x, string t) =>
            {
                Action<ParadoxParser> temp;
                if (dictionary.TryGetValue(t, out temp))
                    temp(x);
            };
        }
    }
}
