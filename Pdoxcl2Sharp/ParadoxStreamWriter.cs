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

    public abstract class ParadoxStreamWriter : IDisposable
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

        private int currentIndent;

        public ParadoxStreamWriter(Stream output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output", "Must be able to write data to an object");
            }

            this.Writer = new StreamWriter(output, Globals.ParadoxEncoding);
            this.currentIndent = 0;
        }

        protected TextWriter Writer { get; set; }

        public abstract void Write(string key, string value, ValueWrite valuetype);
        public abstract void WriteLine(string value, ValueWrite valuetype);
        public abstract void WriteLine(string key, string value, ValueWrite valuetype);
        public abstract void WriteLine(string key, DateTime date);
        public abstract void WriteComment(string comment);
        public abstract void Write(string header, IParadoxWrite obj);

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="value">String to be written</param>
        public virtual void Write(string value)
        {
            this.Write(value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a key-value pair to the stream with no special formatting
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="value">Value to be written to the stream</param>
        public virtual void Write(string key, string value)
        {
            this.Write(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string value that is identified by a key followed by a line terminator
        /// </summary>
        /// <param name="key">The text can identify the value</param>
        /// <param name="value">Value to be written</param>
        public virtual void WriteLine(string key, string value)
        {
            this.WriteLine(key, value, ValueWrite.None);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text stream.
        /// </summary>
        /// <param name="value">The string to write. If value is null, only the line termination characters are written</param>
        public virtual void WriteLine(string value)
        {
            this.Write(value, ValueWrite.NewLine);
        }

        /// <summary>
        /// Writes a line terminator to the text stream
        /// </summary>
        public virtual void WriteLine()
        {
            this.Writer.WriteLine();
        }

        /// <summary>
        /// Writes a boolean that is identified by a key.
        /// The boolean is written in a way to conform with how Paradox writes booleans.
        /// </summary>
        /// <param name="key">Key to be written</param>
        /// <param name="val">Boolean to be written</param>
        public virtual void WriteLine(string key, bool val)
        {
            this.WriteLine(key, val ? "yes" : "no");
        }

        /// <summary>
        /// Writes an integer that is identified by a key.
        /// The integer is written in a way to conform with how Paradox writes integers.
        /// </summary>
        /// <param name="key">Key that identifies the value</param>
        /// <param name="val">Value to the be written to the stream</param>
        public virtual void WriteLine(string key, int val)
        {
            this.WriteLine(key, val.ToString());
        }

        /// <summary>
        /// Writes a double that is a identified by a key.
        /// The double is written in a way to conform with how Paradox writes doubles.
        /// </summary>
        /// <param name="key">Key to be written.</param>
        /// <param name="val">Double to be written</param>
        public virtual void WriteLine(string key, double val)
        {
            this.WriteLine(key, val.ToString("#.000"));
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
                this.Writer.Write(ParadoxStreamWriter.tabs[value == "}" ? this.currentIndent - 1 : this.currentIndent]);
            }

            this.UpdateCurrentIndentFromIndentsIn(value);
            this.Writer.Write(type.HasFlag(ValueWrite.Quoted) ? '"' + value + '"' : value);

            if (type.HasFlag(ValueWrite.NewLine))
            {
                this.Writer.WriteLine();
            }
        }

        /// <summary>
        /// Releases all resources held by the writer
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Writer != null)
                {
                    this.Writer.Dispose();
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
                    this.currentIndent--;
                else if (str[i] == '{')
                    this.currentIndent++;
            }
        }
    }
}
