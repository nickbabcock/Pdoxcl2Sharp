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
        /// Initializes a new instance of the <see cref="ParadoxSaver"/> class with the specified <see cref="Stream"/>
        /// </summary>
        /// <param name="output">The stream that will be written to</param>
        public ParadoxSaver(Stream output) 
            : base(output)
        {
            Writer.NewLine = "\r\n";
        }

        public override void Write(string key, string value, ValueWrite valuetype)
        {
            Write(key, ValueWrite.LeadingTabs);
            Writer.Write('=');
            Write(value, valuetype & ~ValueWrite.LeadingTabs);
        }

        public override void WriteLine(string key, string value, ValueWrite valuetype)
        {
            Write(key, value, valuetype | ValueWrite.NewLine);
        }

        public override void WriteLine(string value, ValueWrite valuetype)
        {
            Write(value, valuetype | ValueWrite.NewLine);
        }

        public override void WriteLine(string key, DateTime date)
        {
            WriteLine(key, date.ToParadoxString(), ValueWrite.Quoted);
        }

        public override void WriteComment(string comment)
        {
            WriteLine('#' + comment, ValueWrite.LeadingTabs);
        }

        public override void Write(string header, Action<ParadoxStreamWriter> objWriter)
        {
            if (!string.IsNullOrEmpty(header))
            {
                Write(header, ValueWrite.LeadingTabs);
                WriteLine("=");
            }

            WriteLine("{", ValueWrite.LeadingTabs);
            objWriter(this);
            WriteLine("}", ValueWrite.LeadingTabs);
        }
    }
}
