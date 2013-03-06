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

            parse(output, action, (p) => ParadoxParser.Parse(data, p));
        }
        public ParadoxSaver(TextWriter output, IParadoxFile file, Action<ParadoxSaver, string> action, string filePath, string originalFilePath)
        {
            if (output == null)
                throw new ArgumentNullException("output", "Must provide an output to write data");

            if (file == null)
                throw new ArgumentNullException("file");

            if (action == null)
                throw new ArgumentNullException("action", "Must define an action when saving");

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            if (String.IsNullOrEmpty(originalFilePath))
                throw new ArgumentNullException("originalFilePath");

            parse(output, action, (p) => ParadoxParser.Parse(originalFilePath, p));
        }
        private void parse(TextWriter output, Action<ParadoxSaver, string> action, Action<Action<ParadoxParser, string>> parseAction)
        {
            writer = output;
            builder = new StringBuilder();
            prevIndex = 0;
            prevToken = LexerToken.Untyped;
            Action<ParadoxParser, string> parser = (p, s) =>
            {
                //If the last token was an equals
                //Means that an empty list was encountered 
                if (prevToken == LexerToken.Equals && p.CurrentToken == LexerToken.Equals && prevIndex >= p.CurrentIndex)
                    writer.Write("{}" + Environment.NewLine);

                lastWrite = WriteType.None;
                underlyingParser = p;

                while (prevIndex > p.CurrentIndex && p.CurrentToken != LexerToken.RightCurly)
                {
                    if (prevIndex > 1)
                        writer.Write(new string('\t', prevIndex - 1));
                    writer.Write('}' + Environment.NewLine);
                    prevIndex--;
                }
                action(this, s);

                switch (lastWrite)
                {
                    case WriteType.Single:
                        p.ReadString();
                        break;
                    case WriteType.None:
                        preprocess(p.CurrentToken);

                        if (p.CurrentToken == LexerToken.Equals)
                            writer.Write(new string('\t', p.CurrentIndex));
                        
                        if (p.CurrentToken != LexerToken.Quote)
                            writer.Write(s);
                        else
                        {
                            writer.Write('"');
                            writer.Write(s);
                            writer.Write('"');
                        }

                        if (prevIndex > p.CurrentIndex)
                        {
                            writer.Write("} ");
                            prevIndex--;
                        }

                        if (p.CurrentToken == LexerToken.Equals)
                            writer.Write('=');
                        else if ((p.CurrentToken == LexerToken.Untyped || p.CurrentToken == LexerToken.Quote)
                            && prevToken == LexerToken.Equals
                            && prevIndex >= underlyingParser.CurrentIndex)
                            writer.Write(Environment.NewLine);
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
            parseAction(parser);


            //If the last token was an equals
            //Means that an empty list was encountered 
            if (prevToken == LexerToken.Equals)
                writer.Write("{}");
            if (prevIndex != 0)
                writer.Write('}');
        }
        private void preprocess(LexerToken token)
        {
            if (prevIndex < underlyingParser.CurrentIndex)
            {
                writer.Write('{');
                if (token == LexerToken.Equals)
                    writer.Write(Environment.NewLine);
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
                builder.Append(Environment.NewLine);
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
                builder.Append(Environment.NewLine);
            writer.Write(builder.ToString());
        }
        private void writeNewLineDelimitedList<T>(IEnumerable<T> list, bool appendNewLine = false, bool quoteWrap = false)
        {
            builder.Append("={");
            builder.Append(Environment.NewLine);

            if (!quoteWrap)
            {
                foreach (var value in list)
                {
                    builder.Append('\t', underlyingParser.CurrentIndex + 1);
                    builder.Append(value.ToString());
                    builder.Append(Environment.NewLine);
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
                    builder.Append(Environment.NewLine);
                }
            }
            builder.Append('\t', underlyingParser.CurrentIndex);
            builder.Append('}');
            if (appendNewLine)
                builder.Append(Environment.NewLine);
            writer.Write(builder.ToString());
        }
    }
}
