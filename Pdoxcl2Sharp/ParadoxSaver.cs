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
            this.Writer.NewLine = "\r\n";
        }

        public override void Write(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, ValueWrite.LeadingTabs);
            this.Writer.Write('=');
            this.Write(value, valuetype & ~ValueWrite.LeadingTabs);
        }

        public override void WriteLine(string key, string value, ValueWrite valuetype)
        {
            this.Write(key, value, valuetype | ValueWrite.NewLine);
        }

        public override void WriteLine(string value, ValueWrite valuetype)
        {
            this.Write(value, valuetype | ValueWrite.NewLine);
        }

        public override void WriteLine(string key, DateTime date)
        {
            this.WriteLine(key, date.ToParadoxString(), ValueWrite.Quoted);
        }

        public override void WriteComment(string comment)
        {
            this.WriteLine('#' + comment, ValueWrite.LeadingTabs);
        }

        public override void Write(string header, Action<ParadoxStreamWriter> objWriter)
        {
            this.Write(header, ValueWrite.LeadingTabs);
            this.WriteLine("=");
            this.WriteLine("{", ValueWrite.LeadingTabs);
            objWriter(this);
            this.WriteLine("}", ValueWrite.LeadingTabs);
        }
    }
}
