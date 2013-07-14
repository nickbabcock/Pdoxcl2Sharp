using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    [Flags]
    public enum ValueWrite
    {
        None = 1,
        Quoted = 1 << 1,
        NewLine = 1 << 2,
        LeadingTabs = 1 << 3
    }

    public class ParadoxSaver
    {
        private static string[] tabs = 
        {
            string.Empty,
            new string('\t', 1),
            new string('\t', 2),
            new string('\t', 3),
            new string('\t', 4),
            new string('\t', 5),
            new string('\t', 6),
            new string('\t', 7),
            new string('\t', 8)
        };

        private TextWriter writer;
        private int currentIndent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxSaver"/> class with the specified <see cref="TextWriter"/>
        /// </summary>
        /// <param name="output">The stream that will be written to</param>
        public ParadoxSaver(TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output", "Must be able to write data to an object");
            }
            else if (output.Encoding.WindowsCodePage != Globals.WindowsCodePage && output.Encoding.WindowsCodePage != Globals.UTF16CodePage)
            {
                string err = string.Format("Stream encoding page must be {0} or {1}", Globals.WindowsCodePage, Globals.UTF16CodePage);
                throw new ArgumentException("output", err);
            }

            this.writer = output;
            this.currentIndent = 0;
        }

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="value">String to be written</param>
        public void Write(string value)
        {
            this.Write(value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string dictated by a format
        /// </summary>
        /// <param name="value">String to be written</param>
        /// <param name="type">Denotes what modifications to be made on the string before being written</param>
        public void Write(string value, ValueWrite type)
        {
            if (type.HasFlag(ValueWrite.LeadingTabs))
            {
                this.writer.Write(tabs[value == "}" ? this.currentIndent - 1 : this.currentIndent]);
            }

            this.UpdateCurrentIndentFromIndentsIn(value);
            this.writer.Write(type.HasFlag(ValueWrite.Quoted) ? '"' + value + '"' : value);

            if (type.HasFlag(ValueWrite.NewLine))
            {
                this.writer.WriteLine();
            }
        }

        /// <summary>
        /// Writes a key-value pair to the stream with no special formatting
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        public void Write(string key, string value)
        {
            this.Write(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a key-value pair to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream.</param>
        public void Write(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, ValueWrite.LeadingTabs);
            this.writer.Write('=');
            this.Write(value, valuetype & ~ValueWrite.LeadingTabs);
        }

        /// <summary>
        /// Writes a key-value pair followed by a line terminator to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream</param>
        public void WriteLine(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, value, valuetype | ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a string value that is identified by a key followed by a line terminator
        /// </summary>
        /// <param name="key">The text can identify the value</param>
        /// <param name="value">Value to be written</param>
        public void WriteLine(string key, string value)
        {
            this.WriteLine(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string in the format of <paramref name="valuetype"/> followed by a line terminator.
        /// </summary>
        /// <param name="value">The string to be written to the text stream</param>
        /// <param name="valuetype">The format of how the string should be written</param>
        public void WriteLine(string value, ValueWrite valuetype)
        {
            this.Write(value, valuetype | ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text stream.
        /// </summary>
        /// <param name="value">The string to write. If value is null, only the line termination characters are written</param>
        public void WriteLine(string value)
        {
            this.Write(value, ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a line terminator to the text stream
        /// </summary>
        public void WriteLine()
        {
            this.writer.WriteLine();
        }

        /// <summary>
        /// Writes a date and time that is identified by a key.
        /// The date is written in a way to conform with how Paradox writes dates
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="date">Date to be written</param>
        public void WriteLine(string key, DateTime date)
        {
            this.WriteLine(key, date.ToString("yyyy.M.d"), ValueWrite.Quoted);
        }

        /// <summary>
        /// Writes a boolean that is identified by a key.
        /// The boolean is written in a way to conform with how Paradox writes booleans.
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="val">Boolean to be written</param>
        public void WriteLine(string key, bool val)
        {
            this.WriteLine(key, val ? "yes" : "no");
        }

        /// <summary>
        /// Writes an integer that is identified by a key.
        /// The integer is written in a way to conform with how Paradox writes integers.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="val">Value to the be written to the stream</param>
        public void WriteLine(string key, int val)
        {
            this.WriteLine(key, val.ToString());
        }

        /// <summary>
        /// Writes a double that is a identified by a key.
        /// The double is written in a way to conform with how Paradox writes doubles.
        /// </summary>
        /// <param name="key">Key to be written.</param>
        /// <param name="val">Double to be written</param>
        public void WriteLine(string key, double val)
        {
            this.WriteLine(key, val.ToString("#.000"));
        }

        /// <summary>
        /// Writes a string followed by a line terminator that will be ignored by a paradox parser
        /// </summary>
        /// <param name="comment">The string to be written to the file but ignored on parse</param>
        public void WriteComment(string comment)
        {
            this.Write('#' + comment, ValueWrite.LeadingTabs | ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a structure that is identified by a header
        /// </summary>
        /// <param name="header">The string that will identify the following structure</param>
        /// <param name="obj">The <see cref="IParadoxWrite"/> that dictates how the structure will be written</param>
        public void Write(string header, IParadoxWrite obj)
        {
            this.Write(header, ValueWrite.LeadingTabs);
            this.WriteLine("=");
            this.WriteLine("{", ValueWrite.LeadingTabs);
            obj.Write(this);
            this.WriteLine("}", ValueWrite.LeadingTabs);
        }

        private void UpdateCurrentIndentFromIndentsIn(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '}')
                    this.currentIndent--;
                else if (str[i] == '{')
                    this.currentIndent++;
            }
        }
    }
}
