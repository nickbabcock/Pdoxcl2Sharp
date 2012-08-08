using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nectarine.Test
{
    public static class ExtensionMethods
    {
        public static byte[] ToByteArray(this string str)
        {
            return str.Select(x => (byte)x).ToArray();
            //byte[] bytes = new byte[str.Length * sizeof(char)];
            //System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            //return bytes;
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
