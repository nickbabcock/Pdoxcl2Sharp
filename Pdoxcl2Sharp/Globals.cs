using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public class Globals
    {
        public const int WindowsCodePage = 1252;
        public const int UTF16CodePage = 1200;

        public static readonly Encoding ParadoxEncoding = Encoding.GetEncoding(1252);
    }
}
