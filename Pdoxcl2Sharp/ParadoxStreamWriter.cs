using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Options to control how something is written to a stream.  Different
    /// combinations of options can be created using bitwise operations. The
    /// exception is the None option.  
    /// </summary>
    [Flags]
    public enum ValueWrite
    {
        /// <summary>
        /// Value is written to stream without any modification
        /// </summary>
        None = 0,

        /// <summary>
        /// Wrap the value to be written to the stream in quotes
        /// </summary>
        Quoted = 1 << 0,

        /// <summary>
        /// After writing the value to stream, append a newline
        /// </summary>
        NewLine = 1 << 1,

        /// <summary>
        /// Before writing the value to stream, prepend tabs such that the value
        /// written to stream matches the indent of surrounding values
        /// </summary>
        LeadingTabs = 1 << 2
    }

    public abstract class ParadoxStreamWriter : IDisposable
    {
        private const string DoubleFmt = "0.000";

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

        private int currentIndent;

        protected ParadoxStreamWriter(Stream output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output", "Must be able to write data to an object");
            }

            Writer = new StreamWriter(output, Globals.ParadoxEncoding);
            currentIndent = 0;
        }

        protected TextWriter Writer { get; set; }

        /// <summary>
        /// Writes a key-value pair to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream.</param>
        public abstract void Write(string key, string value, ValueWrite valuetype);

        /// <summary>
        /// Writes a string in the format of <paramref name="valuetype"/> followed by a line terminator.
        /// </summary>
        /// <param name="value">The string to be written to the text stream</param>
        /// <param name="valuetype">The format of how the string should be written</param>
        public abstract void WriteLine(string value, ValueWrite valuetype);

        /// <summary>
        /// Writes a key-value pair followed by a line terminator to the stream with formatting specified by <paramref name="valuetype"/>.
        /// It is assumed that the key will be indented and that the value will not be.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        /// <param name="valuetype">Dictates how the value should be written to the stream</param>
        public abstract void WriteLine(string key, string value, ValueWrite valuetype);

        /// <summary>
        /// Writes a date and time that is identified by a key.
        /// The date is written in a way to conform with how Paradox writes dates
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="date">Date to be written</param>
        public abstract void WriteLine(string key, DateTime date);

        /// <summary>
        /// Writes a string followed by a line terminator that will be ignored by a paradox parser
        /// </summary>
        /// <param name="comment">The string to be written to the file but ignored on parse</param>
        public abstract void WriteComment(string comment);

        /// <summary>
        /// Writes a structure that is identified by a header
        /// </summary>
        /// <param name="header">The string that will identify the following structure</param>
        /// <param name="obj">The <see cref="IParadoxWrite"/> that dictates how the structure will be written</param>
        public void Write(string header, IParadoxWrite obj)
        {
            Write(header, obj.Write);
        }

        /// <summary>
        /// Writes a structure that is identified by a header
        /// </summary>
        /// <param name="header">The string that will identify the following structure</param>
        /// <param name="objWriter">
        /// A function that accepts a <see cref="ParadoxStreamWriter"/> 
        /// and dictates how the structure will be written
        /// </param>
        public abstract void Write(string header, Action<ParadoxStreamWriter> objWriter);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="value">String to be written</param>
        public virtual void Write(string value)
        {
            Write(value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a double to the stream
        /// </summary>
        /// <param name="value">Double to be written</param>
        public virtual void Write(double value)
        {
            Write(value.ToString(DoubleFmt));
        }

        /// <summary>
        /// Writes an integer to the stream
        /// </summary>
        /// <param name="value">Integer to be written</param>
        public virtual void Write(int value)
        {
            Write(value.ToString());
        }

        /// <summary>
        /// Writes a DateTime to the stream
        /// </summary>
        /// <param name="value">DateTime to be written</param>
        public virtual void Write(DateTime value)
        {
            Write(value.ToParadoxString());
        }

        /// <summary>
        /// Writes a key-value pair to the stream with no special formatting
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        public virtual void Write(string key, string value)
        {
            Write(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string value that is identified by a key followed by a line terminator
        /// </summary>
        /// <param name="key">The text can identify the value</param>
        /// <param name="value">Value to be written</param>
        public virtual void WriteLine(string key, string value)
        {
            WriteLine(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text stream.
        /// </summary>
        /// <param name="value">The string to write. If value is null, only the line termination characters are written</param>
        public virtual void WriteLine(string value)
        {
            Write(value, ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a line terminator to the text stream
        /// </summary>
        public virtual void WriteLine()
        {
            Writer.WriteLine();
        }

        /// <summary>
        /// Writes a boolean that is identified by a key.
        /// The boolean is written in a way to conform with how Paradox writes booleans.
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="val">Boolean to be written</param>
        public virtual void WriteLine(string key, bool val)
        {
            WriteLine(key, val ? "yes" : "no");
        }

        /// <summary>
        /// Writes an integer that is identified by a key.
        /// The integer is written in a way to conform with how Paradox writes integers.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="val">Value to the be written to the stream</param>
        public virtual void WriteLine(string key, int val)
        {
            WriteLine(key, val.ToString());
        }

        /// <summary>
        /// Writes a double that is a identified by a key.
        /// The double is written in a way to conform with how Paradox writes doubles.
        /// </summary>
        /// <param name="key">Key to be written.</param>
        /// <param name="val">Double to be written</param>
        public virtual void WriteLine(string key, double val)
        {
            WriteLine(key, val.ToString(DoubleFmt));
        }

        /// <summary>
        /// Writes a string dictated by a format
        /// </summary>
        /// <param name="value">String to be written</param>
        /// <param name="type">Denotes what modifications to be made on the string before being written</param>
        public virtual void Write(string value, ValueWrite type)
        {
            if (type.HasFlag(ValueWrite.LeadingTabs))
            {
                Writer.Write(ParadoxStreamWriter.tabs[value == "}" ? currentIndent - 1 : currentIndent]);
            }

            UpdateCurrentIndentFromIndentsIn(value);
            Writer.Write(type.HasFlag(ValueWrite.Quoted) ? '"' + value + '"' : value);

            if (type.HasFlag(ValueWrite.NewLine))
            {
                Writer.WriteLine();
            }
        }

        /// <summary>
        /// Releases all resources held by the writer
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Writer != null)
                {
                    Writer.Dispose();
                }
            }
        }

        /// <summary>
        /// Given a string, the function will detect squirrely brackets and update
        /// the current indent of the writer
        /// </summary>
        /// <param name="str">String to be searched for squirrely brackets</param>
        private void UpdateCurrentIndentFromIndentsIn(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '}')
                    currentIndent--;
                else if (str[i] == '{')
                    currentIndent++;
            }
        }
    }
}
