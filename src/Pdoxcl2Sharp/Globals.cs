using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public static class Globals
    {
        public static readonly Encoding ParadoxEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a string that adheres to
        /// Paradox's own encodings
        /// </summary>
        /// <param name="date">Date to be converted into a string</param>
        /// <returns>The Paradox string representation of the date</returns>
        public static string ToParadoxString(this DateTime date)
        {
            return string.Format("{0}.{1}.{2}", date.Year, date.Month, date.Day);
        }
    }
}
