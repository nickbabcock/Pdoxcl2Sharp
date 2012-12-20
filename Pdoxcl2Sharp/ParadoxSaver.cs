using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pdoxcl2Sharp
{
    public class ParadoxSaver
    {
        private enum WriteType
        {
            Single,
            List,
            None
        }
        private TextWriter writer;
        private ParadoxParser underlyingParser;
        private WriteType lastWrite;
        private StringBuilder builder;
        private int prevIndex;
        private LexerToken prevToken;
        public ParadoxSaver(TextWriter output, byte[] data, Action<ParadoxSaver, string> action)
        {
            if (output == null)
                throw new ArgumentNullException("output", "Must provide an output to write data");

            if (action == null)
                throw new ArgumentNullException("action", "Must define an action when saving");

            writer = output;
            builder = new StringBuilder();
            prevIndex = 0;
            prevToken = LexerToken.Untyped;
            Action<ParadoxParser, string> parser = (p, s) =>
                {
                    lastWrite = WriteType.None;
                    underlyingParser = p;
                    action(this, s);


                    switch (lastWrite)
                    {
                        case WriteType.Single:
                            p.ReadString();
                            break;
                        case WriteType.None:
                            preprocess(p.CurrentToken);

                            if (p.CurrentToken == LexerToken.Equals)
                                writer.Write(new string('\t', underlyingParser.CurrentIndex));
                            writer.Write(s);

                            if (prevIndex > underlyingParser.CurrentIndex)
                                writer.Write("} ");

                            if (p.CurrentToken == LexerToken.Equals)
                                writer.Write('=');
                            else if (p.CurrentToken == LexerToken.Untyped && prevToken == LexerToken.Equals
                                && prevIndex >= underlyingParser.CurrentIndex)
                                writer.Write(writer.NewLine);
                            else if (p.CurrentToken == LexerToken.Untyped)
                                writer.Write(' ');
                            break;
                        case WriteType.List:
                            p.ReadStringList();
                            break;
                    }
                    prevIndex = underlyingParser.CurrentIndex;
                    prevToken = underlyingParser.CurrentToken;
                };
            ParadoxParser.Parse(data, parser);
            if (prevIndex != 0)
                writer.Write('}');
        }
        private void preprocess(LexerToken token)
        {
            if (prevIndex < underlyingParser.CurrentIndex)
            {
                writer.Write('{');
                if (token == LexerToken.Equals)
                    writer.Write(writer.NewLine);
            }
        }
        public void WriteValue(string value, bool appendNewLine = false, bool quoteWrap = false)
        {
            builder.Clear();
            lastWrite = WriteType.Single;
            preprocess(prevToken);

            builder.Append('\t', underlyingParser.CurrentIndex);
            builder.Append(underlyingParser.CurrentString);
            builder.Append('=');
            if (quoteWrap)
            {
                builder.Append('"');
                builder.Append(value);
                builder.Append('"');
            }
            else
            {
                builder.Append(value);
            }
            if (appendNewLine)
                builder.Append(writer.NewLine);
            writer.Write(builder.ToString());
        }

        public void WriteList<T>(IEnumerable<T> list, bool appendNewLine = false,
            string delimiter = " ", bool quoteWrap = false)
        {
            builder.Clear();
            lastWrite = WriteType.List;
            preprocess(prevToken);


            builder.Append('\t', underlyingParser.CurrentIndex);
            builder.Append(underlyingParser.CurrentString);


            if (delimiter.Contains('\r') || delimiter.Contains('\n'))
            {
                writeNewLineDelimitedList(list, appendNewLine: appendNewLine, quoteWrap: quoteWrap);
                return;
            }

            builder.Append("={ ");

            if (!quoteWrap)
            {
                foreach (var value in list)
                {
                    builder.Append(value.ToString());
                    builder.Append(delimiter);
                }
            }
            else
            {
                foreach (var value in list)
                {
                    builder.Append('"');
                    builder.Append(value.ToString());
                    builder.Append('"');
                    builder.Append(delimiter);
                }
            }

            builder = builder.Remove(builder.Length - delimiter.Length, delimiter.Length);

            builder.Append(" }");
            if (appendNewLine)
                builder.Append(writer.NewLine);
            writer.Write(builder.ToString());
        }
        private void writeNewLineDelimitedList<T>(IEnumerable<T> list, bool appendNewLine = false, bool quoteWrap = false)
        {
            builder.Append("={");
            builder.Append(writer.NewLine);

            if (!quoteWrap)
            {
                foreach (var value in list)
                {
                    builder.Append('\t', underlyingParser.CurrentIndex + 1);
                    builder.Append(value.ToString());
                    builder.Append(writer.NewLine);
                }
            }
            else
            {
                foreach (var value in list)
                {
                    builder.Append('\t', underlyingParser.CurrentIndex + 1);
                    builder.Append('"');
                    builder.Append(value.ToString());
                    builder.Append('"');
                    builder.Append(writer.NewLine);
                }
            }
            builder.Append('\t', underlyingParser.CurrentIndex);
            builder.Append('}');
            if (appendNewLine)
                builder.Append(writer.NewLine);
            writer.Write(builder.ToString());
        }
    }
}
