using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Defines a common interface for parsing content in Paradox's format
    /// </summary>
    public interface IParadoxRead
    {
        /// <summary>
        /// This function is invoked as a callback when the parser encounters
        /// a token and does not know how to handle it.
        /// </summary>
        /// <param name="parser">The parser that invoked the callback</param>
        /// <param name="token">The token that the parser didn't know how to handle</param>
        void TokenCallback(ParadoxParser parser, string token);
    }
}
