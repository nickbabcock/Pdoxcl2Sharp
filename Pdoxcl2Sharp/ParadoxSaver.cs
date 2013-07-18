using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public class ParadoxSaver : ParadoxStreamWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxSaver"/> class with the specified <see cref="TextWriter"/>
        /// </summary>
        /// <param name="output">The stream that will be written to</param>
        public ParadoxSaver(Stream output) 
            : base(output)
        {
            this.Writer.NewLine = "\r\n";
        }

        /// <summary>
        /// Writes a key-value pair to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream.</param>
        public override void Write(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, ValueWrite.LeadingTabs);
            this.Writer.Write('=');
            this.Write(value, valuetype & ~ValueWrite.LeadingTabs);
        }

        /// <summary>
        /// Writes a key-value pair followed by a line terminator to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream</param>
        public override void WriteLine(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, value, valuetype | ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a string in the format of <paramref name="valuetype"/> followed by a line terminator.
        /// </summary>
        /// <param name="value">The string to be written to the text stream</param>
        /// <param name="valuetype">The format of how the string should be written</param>
        public override void WriteLine(string value, ValueWrite valuetype)
        {
            this.Write(value, valuetype | ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a date and time that is identified by a key.
        /// The date is written in a way to conform with how Paradox writes dates
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="date">Date to be written</param>
        public override void WriteLine(string key, DateTime date)
        {
            this.WriteLine(key, date.ToString("yyyy.M.d"), ValueWrite.Quoted);
        }

        /// <summary>
        /// Writes a string followed by a line terminator that will be ignored by a paradox parser
        /// </summary>
        /// <param name="comment">The string to be written to the file but ignored on parse</param>
        public override void WriteComment(string comment)
        {
            this.WriteLine('#' + comment, ValueWrite.LeadingTabs);
        }

        /// <summary>
        /// Writes a structure that is identified by a header
        /// </summary>
        /// <param name="header">The string that will identify the following structure</param>
        /// <param name="obj">The <see cref="IParadoxWrite"/> that dictates how the structure will be written</param>
        public override void Write(string header, IParadoxWrite obj)
        {
            this.Write(header, ValueWrite.LeadingTabs);
            this.WriteLine("=");
            this.WriteLine("{", ValueWrite.LeadingTabs);
            obj.Write(this);
            this.WriteLine("}", ValueWrite.LeadingTabs);
        }
    }
}
