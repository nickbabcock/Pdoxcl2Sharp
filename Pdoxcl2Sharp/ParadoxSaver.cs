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
            None,
            Skip
        }

        private TextWriter writer;
        private ParadoxParser underlyingParser;
        private WriteType lastWrite;
        private StringBuilder builder;
        private int prevIndex;
        private LexerToken prevToken;

        public int CurrentIndex { get { return underlyingParser.CurrentIndex; } }


        public ParadoxSaver(TextWriter output, Stream data, Action<ParadoxSaver, string> action)
        {
            if (output == null)
                throw new ArgumentNullException("output", "Must provide an output to write data");

            if (action == null)
                throw new ArgumentNullException("action", "Must define an action when saving");

            parse(output, action, (p) => ParadoxParser.Parse(data, p));
        }

        public ParadoxSaver(TextWriter output, IParadoxParse file, Action<ParadoxSaver, string> action, string filePath, string originalFilePath)
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
            parseAction(wrapUnderlyingAction(action));


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
                if (token == LexerToken.Equals || lastWrite == WriteType.Single)
                    writer.Write(Environment.NewLine);
            }
        }

        private void writeInner(string value, bool isValueWrite, bool appendNewLine = false, bool quoteWrap = false)
        {
            builder.Clear();
            lastWrite = WriteType.Single;
            preprocess(prevToken);

            builder.Append('\t', underlyingParser.CurrentIndex);
            if (isValueWrite)
            {
                builder.Append(underlyingParser.CurrentString);
                builder.Append('=');
            }
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

        public void WriteValue(string value, bool appendNewLine = false, bool quoteWrap = false)
        {
            writeInner(value, true, appendNewLine, quoteWrap);
        }

        public void Write(string value, bool appendNewLine = false, bool quoteWrap = false)
        {
            writeInner(value, false, appendNewLine, quoteWrap);
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


        private Action<ParadoxParser, string> wrapUnderlyingAction(Action<ParadoxSaver, string> action)
        {
            return (ParadoxParser parser, string token) =>
                {
                    //If the last token was an equals
                    //Means that an empty list was encountered 
                    if (prevToken == LexerToken.Equals && parser.CurrentToken == LexerToken.Equals && prevIndex >= parser.CurrentIndex)
                        writer.Write("{}" + Environment.NewLine);

                    lastWrite = WriteType.None;
                    underlyingParser = parser;

                    while (prevIndex > parser.CurrentIndex && parser.CurrentToken != LexerToken.RightCurly)
                    {
                        if (prevIndex > 1)
                            writer.Write(new string('\t', prevIndex - 1));
                        writer.Write('}' + Environment.NewLine);
                        prevIndex--;
                    }
                    action(this, token);

                    switch (lastWrite)
                    {
                        case WriteType.Single:
                            parser.ReadString();
                            break;
                        case WriteType.None:
                            preprocess(parser.CurrentToken);

                            if (parser.CurrentToken == LexerToken.Equals)
                                writer.Write(new string('\t', parser.CurrentIndex));

                            if (parser.CurrentToken != LexerToken.Quote)
                                writer.Write(token);
                            else
                            {
                                writer.Write('"');
                                writer.Write(token);
                                writer.Write('"');
                            }

                            if (prevIndex > parser.CurrentIndex)
                            {
                                writer.Write("} ");
                                prevIndex--;
                            }

                            if (parser.CurrentToken == LexerToken.Equals)
                                writer.Write('=');
                            else if ((parser.CurrentToken == LexerToken.Untyped || parser.CurrentToken == LexerToken.Quote)
                                && prevToken == LexerToken.Equals
                                && prevIndex >= underlyingParser.CurrentIndex)
                                writer.Write(Environment.NewLine);
                            else if (parser.CurrentToken == LexerToken.Untyped)
                                writer.Write(' ');
                            break;
                        case WriteType.List:
                            parser.ReadStringList();
                            break;
                    }
                    prevIndex = underlyingParser.CurrentIndex;
                    prevToken = underlyingParser.CurrentToken;
                };
        }
        public void Parse(Action<ParadoxSaver, string> action)
        {
            writer.Write(new string('\t', CurrentIndex) + underlyingParser.CurrentString + '=');
            underlyingParser.Parse(wrapUnderlyingAction(action));
            writer.WriteLine(new string('\t', CurrentIndex) + '}');
        }
        public void SkipNext()
        {
            underlyingParser.ReadString();
            lastWrite = WriteType.Skip;
        }
    }
}
