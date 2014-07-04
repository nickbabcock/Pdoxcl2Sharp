using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public class ParadoxCompressedSaver : ParadoxStreamWriter
    {
        public ParadoxCompressedSaver(Stream data)
            : base(data)
        {
            Writer.NewLine = "\n";
        }

        public override void Write(string value, ValueWrite type)
        {
            base.Write(value, Normalize(type));
        }

        public override void Write(string key, string value, ValueWrite valuetype)
        {
            Write(key);
            Write("=");
            Write(value, valuetype & ~ValueWrite.LeadingTabs);
        }

        public override void WriteLine(string key, string value, ValueWrite valuetype)
        {
            Write(key, value, Normalize(valuetype) | ValueWrite.NewLine);
        }

        public override void WriteLine(string value, ValueWrite valuetype)
        {
            Write(value, Normalize(valuetype) | ValueWrite.NewLine);
        }

        public override void WriteLine(string key, DateTime date)
        {
            Write(key, date.ToParadoxString(), ValueWrite.Quoted);
        }

        public override void WriteComment(string comment)
        {
            Write('#' + comment, ValueWrite.NewLine);
        }

        public override void Write(string header, Action<ParadoxStreamWriter> objWriter)
        {
            Write(header);
            Write("=");
            Write("{");
            objWriter(this);
            Write("}");
        }

        private ValueWrite Normalize(ValueWrite val)
        {
            return StripLeadingTabs(NoNewLineIfQuoted(val));
        }

        /// <summary>
        /// If a value being written is quoted, then there is no need to insert
        /// a newline because quotes are used as delimiters
        /// </summary>
        /// <param name="val"><see cref="ValueWrite"/> to test against</param>
        /// <returns>A new <see cref="ValueWrite"/> that ensures delimitation</returns>
        private ValueWrite NoNewLineIfQuoted(ValueWrite val)
        {
            return val.HasFlag(ValueWrite.Quoted) ? val & ~ValueWrite.NewLine : val;
        }

        private ValueWrite StripLeadingTabs(ValueWrite val)
        {
            return val & ~ValueWrite.LeadingTabs;
        }
    }
}
