using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public static class Globals
    {
        public const int WindowsCodePage = 1252;
        public static readonly Encoding ParadoxEncoding = Encoding.GetEncoding(WindowsCodePage);

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

        public static Type GenericTypeImplementation(this Type type, Type genericInterface)
        {
            foreach (var face in type.GetAllInterfaces())
            {
                if (face.IsGenericType && face.GetGenericTypeDefinition() == genericInterface)
                {
                    return face;
                }
            }

            return null;
        }

        public static IEnumerable<Type> GetAllInterfaces(this Type type)
        {
            if (type.IsInterface)
            {
                yield return type;
            }

            foreach (var face in type.GetInterfaces())
            {
                yield return face;
            }
        }
    }
}
