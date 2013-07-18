using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Defines a class that can output its content in a format that 
    /// can be later used by Paradox
    /// </summary>
    public interface IParadoxWrite
    {
        /// <summary>
        /// Outputs the class's contents to the writer.
        /// </summary>
        /// <param name="writer">Object to write the structure of the class</param>
        void Write(ParadoxStreamWriter writer);
    }
}
