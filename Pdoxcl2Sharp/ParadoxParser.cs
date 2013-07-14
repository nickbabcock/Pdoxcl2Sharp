using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public enum LexerToken
    {
        Equals,
        Quote,
        LeftCurly,
        RightCurly,
        LeftParanthesis,
        RightParanthesis,
        Comment,
        Comma,
        Untyped
    }

    public class ParadoxParser
    {
        private const int MaxTokenSize = 256;
        private const int BufferSize = 0x8000;
        private const NumberStyles SignedFloatingStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        private int currentIndent;
        private LexerToken currentToken;
        private LexerToken? nextToken;
        private char currentByte;
        private int currentPosition;
        private int bufferSize;
        private char[] buffer = new char[BufferSize];
        private char[] stringBuffer = new char[MaxTokenSize];
        private int stringBufferCount = 0;
        private Stream stream;
        private StreamReader reader;
        private bool eof = false;
        private string currentString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxParser"/> class.
        /// Parses a stream and executes the parsing strategy when an unknown token is encountered.
        /// </summary>
        /// <param name="data">Stream to be parsed</param>
        /// <param name="parseStrategy">The strategy to be invoked when an unknown token is encountered</param>
        /// <exception cref="ArgumentNullException">If any of the parameters are null</exception>
        public ParadoxParser(Stream data, Action<ParadoxParser, string> parseStrategy)
        {
            this.Init(data, parseStrategy);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxParser"/> class.
        /// Immediately parses the entity and populates the <see cref="IParadoxRead"/>
        /// </summary>
        /// <param name="parser">The paradox structure that will be populated from the stream</param>
        /// <param name="data">The stream that will be parsed</param>
        /// <exception cref="ArgumentNullException">If any of the parameters are null</exception>
        public ParadoxParser(IParadoxRead parser, Stream data)
        {
            if (parser == null)
                throw new ArgumentNullException("parser");

            this.Init(data, parser.TokenCallback);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxParser"/> class.
        /// Immediately parses the file and populates the <see cref="IParadoxRead"/>
        /// </summary>
        /// <param name="file">The paradox structure that will be populated from the file</param>
        /// <param name="filePath">The file path that will be parsed</param>
        /// <exception cref="ArgumentNullException">If any of the parameters are null</exception>
        public ParadoxParser(IParadoxRead file, string filePath)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            this.Init(filePath, file.TokenCallback);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxParser"/> class.
        /// Immediately parses the file, executing the parsing strategy when an unknown token is encountered.
        /// </summary>
        /// <param name="filePath">The file path that will be parsed</param>
        /// <param name="parseStrategy">The function that will be called when there is a unknown token encountered</param>
        /// <exception cref="ArgumentNullException">If any of the parameters are null</exception>
        public ParadoxParser(string filePath, Action<ParadoxParser, string> parseStrategy)
        {
            this.Init(filePath, parseStrategy);
        }

        /// <summary>
        /// Gets how many indents the current parser is at.  For instance if the parser read
        /// read two '{' but zero '}', the <see cref="CurrentIndent"/> would be two.
        /// </summary>
        public int CurrentIndent
        {
            get { return this.currentIndent; }
        }

        /// <summary>
        /// Returns the equivalent System.DateTime as the input string.  For the string
        /// to be a valid date it must be formatted as (year).(month).(day), where
        /// integers represent the actual year and not abbreviations.
        /// </summary>
        /// <param name="dateTime">A string containing the date to parse.</param>
        /// <param name="result">Contains the equivalent System.DateTime as the input parameter</param>
        /// <returns>True if the conversion was successful</returns>
        public static bool TryParseDate(string dateTime, out DateTime result)
        {
            result = DateTime.MinValue;
            string[] splitted = dateTime.Split('.');
            if (splitted.Length != 3)
                return false;
            int year, month, day;
            if (!int.TryParse(splitted[0], NumberStyles.None, CultureInfo.InvariantCulture, out year))
                return false;
            if (!int.TryParse(splitted[1], NumberStyles.None, CultureInfo.InvariantCulture, out month))
                return false;
            if (!int.TryParse(splitted[2], NumberStyles.None, CultureInfo.InvariantCulture, out day))
                return false;

            if ((year < 1 || year > 9999) || (month < 1 || month > 12) || (day < 1 || day > DateTime.DaysInMonth(year, month)))
                return false;
            result = new DateTime(year, month, day);
            return true;
        }

        /// <summary>
        /// Checks to see whether a given byte is considered a whitespace: space, horizontal tab,
        /// newline, vertical tab, feed, or carriage return
        /// </summary>
        /// <param name="c">The byte that will be tested if it is whitespace</param>
        /// <returns>True if the byte is whitespace</returns>
        public static bool IsSpace(char c)
        {
            return c == ' ' || (c >= '\t' && c <= '\r');
        }

        public static void Parse(Stream data, Action<ParadoxParser, string> parseStrategy)
        {
            ParadoxParser p = new ParadoxParser(data, parseStrategy);
        }

        public static void Parse(IParadoxRead entity, Stream data)
        {
            ParadoxParser p = new ParadoxParser(entity, data);
        }

        public static void Parse(IParadoxRead file, string filePath)
        {
            ParadoxParser p = new ParadoxParser(file, filePath);
        }

        public static void Parse(string filePath, Action<ParadoxParser, string> parseStrategy)
        {
            ParadoxParser p = new ParadoxParser(filePath, parseStrategy);
        }

        /// <summary>
        /// Used to parse a set of tokens that are contained within curly brackets.
        /// For example, if a file contains a set of countries and each country was an <see cref="IParadoxRead"/>,
        /// the file would invoke this method whenever it found a new country.
        /// </summary>
        /// <param name="innerStructure">Defines how to parse the inner set of tokens.</param>
        /// <typeparam name="T">Type of the structure to parse</typeparam>
        /// <returns>The passed in parameter newly parsed</returns>
        public T Parse<T>(T innerStructure) where T : class, IParadoxRead
        {
            Parse(innerStructure.TokenCallback);
            return innerStructure;
        }

        /// <summary>
        /// Applies an an action on each token found while parsing the content for the duration
        /// of brackets ('{').  This means that it is an error if this function is called without
        /// the next token being an open bracket.
        /// </summary>
        /// <param name="action">The action to be performed on each token between brackets</param>
        public void Parse(Action<ParadoxParser, string> action)
        {
            this.DoWhileBracket(() => action(this, this.ReadString()));
        }

        /// <summary>
        /// Returns, in string form, the bytes between two tokens, unless the a quote is 
        /// encountered, which then all the bytes between two enclosing quotes will be returned
        /// without the quotes in the return value.  <see cref="CurrentString"/> is set to the 
        /// return value.
        /// </summary>
        /// <returns>String representing the next series of bytes in the stream</returns>
        public string ReadString()
        {
            this.currentToken = this.GetNextToken();
            if (this.currentToken == LexerToken.Untyped)
            {
                do
                {
                    this.stringBuffer[this.stringBufferCount++] = this.currentByte;
                } while (!IsSpace(this.currentByte = this.ReadNext()) && this.SetcurrentToken(this.currentByte) == LexerToken.Untyped && !this.eof);
            }
            else if (this.currentToken == LexerToken.Quote)
            {
                while ((this.currentByte = this.ReadNext()) != '"' && !this.eof)
                    this.stringBuffer[this.stringBufferCount++] = this.currentByte;
            }
            else
            {
                return this.ReadString();
            }

            this.currentString = new string(this.stringBuffer, 0, this.stringBufferCount);
            this.stringBufferCount = 0;
            return this.currentString;
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as an integer
        /// </summary>
        /// <returns>Integer associated with the next series of bytes in the parser</returns>
        public int ReadInt32()
        {
            int result = 0;
            bool negative = false;

            while (this.GetNextToken() != LexerToken.Untyped && !this.eof)
                ;

            if (this.eof)
                return 0;

            do
            {
                if (this.currentByte >= '0' && this.currentByte <= '9')
                    result = (10 * result) + (this.currentByte - '0');
                else if (this.currentByte == '-')
                    negative = true;
            } while (!IsSpace(this.currentByte = this.ReadNext()) && this.SetcurrentToken(this.currentByte) == LexerToken.Untyped && !this.eof);

            return negative ? -result : result;
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a short
        /// </summary>
        /// <returns>Short associated with the next series of bytes in the parser</returns>
        public short ReadInt16() 
        { 
            return (short)this.ReadInt32();
        }
        
        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a signed byte
        /// </summary>
        /// <returns>Signed byte associated with the next series of bytes in the parser</returns>
        public sbyte ReadSByte() 
        { 
            return (sbyte)this.ReadInt32();
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as an unsigned integer.
        /// </summary>
        /// <returns>Unsigned integer associated with the next series of bytes in the parser</returns>
        public uint ReadUInt32()
        {
            uint result = 0;

            while (this.GetNextToken() != LexerToken.Untyped && !this.eof)
                ;

            if (this.eof)
                return 0;

            do
            {
                result = (uint)((10 * result) + (this.currentByte - '0'));
            } while (!IsSpace(this.currentByte = this.ReadNext()) && this.SetcurrentToken(this.currentByte) == LexerToken.Untyped && !this.eof);
            return result;
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as an unsigned short integer.
        /// </summary>
        /// <returns>Unsigned short integer associated with the next series of bytes in the parser</returns>
        public ushort ReadUInt16() 
        { 
            return (ushort)this.ReadUInt32();
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a byte.
        /// </summary>
        /// <returns>Byte associated with the next series of bytes in the parser</returns>
        public byte ReadByte() 
        { 
            return (byte)this.ReadUInt32();
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a double.
        /// </summary>
        /// <returns>Double associated with the next series of bytes in the parser</returns>
        public double ReadDouble()
        {
            double result;
            if (double.TryParse(this.ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "{0} is not a correct Double", this.currentString));
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a Float.
        /// </summary>
        /// <returns>Float associated with the next series of bytes in the parser</returns>
        public float ReadFloat()
        {
            float result;
            if (float.TryParse(this.ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "{0} is not a correct Float", this.currentString));
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a DateTime
        /// </summary>
        /// <returns>System.DateTime associated with next series of bytes in the parser</returns>
        public DateTime ReadDateTime()
        {
            DateTime result;
            if (TryParseDate(this.ReadString(), out result))
                return result;
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "{0} is not a correct DateTime", this.currentString));
        }

        /// <summary>
        /// Reads the data between brackets as integers
        /// </summary>
        /// <returns>A list of the data interpreted as integers</returns>
        public IList<int> ReadIntList() 
        { 
            return this.ReadList(this.ReadInt32);
        }

        /// <summary>
        /// Reads the data between brackets as doubles
        /// </summary>
        /// <returns>A list of the data interpreted as doubles</returns>
        public IList<double> ReadDoubleList() 
        { 
            return this.ReadList(this.ReadDouble);
        }

        /// <summary>
        /// Reads the data between brackets as strings
        /// </summary>
        /// <returns>A list of the data interpreted as strings</returns>
        public IList<string> ReadStringList()
        { 
            return this.ReadList(this.ReadString);
        }

        /// <summary>
        /// Reads the data between brackets as DateTimes
        /// </summary>
        /// <returns>A list of the data interpreted as DateTimes</returns>
        public IList<DateTime> ReadDateTimeList() 
        { 
            return this.ReadList(this.ReadDateTime);
        }

        /// <summary>
        /// Extracts a dictionary from the data that is contained with brackets.
        /// It is assumed that key and value data will be separated by a token.
        /// It is also assumed that the key precedes the value definition.
        /// </summary>
        /// <example>
        /// extensions = 
        /// {
        ///     .log = "log file"
        ///     .txt = "text file"
        /// }
        /// </example>
        /// <typeparam name="TKey">Type of the key of the dictionary</typeparam>
        /// <typeparam name="TValue">Type of the value of the dictionary</typeparam>
        /// <param name="keyFunc">Function that when given the parser will extract a key</param>
        /// <param name="valueFunc">Function that when given the parser will extract a value</param>
        /// <returns>A dictionary that is populated from the data within brackets with the provided functions</returns>
        public IDictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<ParadoxParser, TKey> keyFunc, Func<ParadoxParser, TValue> valueFunc)
        {
            if (keyFunc == null)
                throw new ArgumentNullException("keyFunc", "Function for extracting keys must not be null");
            if (valueFunc == null)
                throw new ArgumentNullException("valueFunc", "Function for extracting values must not be null");
            
            IDictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
            this.DoWhileBracket(() => result.Add(keyFunc(this), valueFunc(this)));
            return result;
        }

        /// <summary>
        /// Advances the parser through the left bracket ('{') and then invokes the action.
        /// It is assumed that the action will consume only what is contained in the brackets.
        /// This function is useful when the number of data within brackets is known but may be of various types.
        /// </summary>
        /// <param name="action">An action that the parser will perform to extract data inside the curly brackets</param>
        public void ReadInsideBrackets(Action<ParadoxParser> action)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action for reading bracket content must not be null");
            this.DoWhileBracket(() => action(this));
        }

        /// <summary>
        /// Returns the associated LexerToken with a given byte.  Compares the byte with 
        /// known ASCII values of the tokens
        /// </summary>
        /// <param name="c">Character to be converted to associated token</param>
        /// <returns>The token associated with parameter</returns>
        private static LexerToken GetToken(char c)
        {
            switch (c)
            {
                case '=':
                    return LexerToken.Equals;
                case '"':
                    return LexerToken.Quote;
                case '{':
                    return LexerToken.LeftCurly;
                case '}':
                    return LexerToken.RightCurly;
                case ')':
                    return LexerToken.LeftParanthesis;
                case '(':
                    return LexerToken.RightParanthesis;
                case '#':
                case '!':
                    return LexerToken.Comment;
                case ',':
                case ';':
                    return LexerToken.Comma;
                default:
                    return LexerToken.Untyped;
            }
        }

        /// <summary>
        /// Parses the date between curly brackets in a manner dictated by the function parameter
        /// </summary>
        /// <typeparam name="T">Type that the data will be interpreted as</typeparam>
        /// <param name="func">Function that will extract the data</param>
        /// <returns>Data between curly brackets constructed as a list.</returns>
        private IList<T> ReadList<T>(Func<T> func)
        {
            List<T> result = new List<T>();
            this.DoWhileBracket(() => result.Add(func()));
            return result;
        }

        /// <summary>
        /// Checks the current, and if needed next token in the stream in an attempt to locate a left curly.  If the token encountered is 
        /// an equality symbol, it will read the next token and see if that is a left curly, e.g. x = { y }.
        /// If the initial read token isn't an equality symbol or a left curly, or if the initial read token is an equality symbol
        /// but the subsequent token isn't a left curly, then an exception is thrown.
        /// </summary>
        private void EnsureLeftCurly()
        {
            if (this.currentToken == LexerToken.LeftCurly)
                return;

            this.currentToken = this.GetNextToken();
            if (this.currentToken == LexerToken.Equals)
                this.currentToken = this.GetNextToken();

            if (this.currentToken != LexerToken.LeftCurly)
                throw new InvalidOperationException("When reading inside brackets the first token must be a left curly");
        }

        /// <summary>
        /// Executes an action while the matching closing right curly '}' is not met.
        /// If the action does not consume all the data inside the bracket content,
        /// the action is repeated.  Before the action is executed, the parser
        /// ensures parsing at the beginning of the data
        /// </summary>
        /// <remarks>
        /// Even if the action is blank, the function will protect against an infinite
        /// loop and will read until the end of the bracket content or if the end of 
        /// the file occurred, whichever one comes first.
        /// <para/>
        /// If the action doesn't advance the underlying stream, the stream will still
        /// advance as long as the loop conditional is true.  For the loop conditional to be
        /// true the current token or the next token can't be a right a curly.
        /// Since the action doesn't advance the stream it is guaranteed that current
        /// iteration's this.currentToken is the same value as the previous iteration's
        /// <see cref="PeekToken"/> and <see cref="PeekToken"/> token could not have been
        /// a right curly.  Therefore if the action doesn't advance the stream,
        /// this.currentToken will never be a right curly and <see cref="PeekToken"/> will
        /// always be invoked. <see cref="PeekToken"/> advances the stream.
        /// Infinite loop impossible
        /// </remarks>
        /// <param name="act">The action that will be repeatedly performed while there is data left in the brackets</param>
        private void DoWhileBracket(Action act)
        {
            int startingIndent = this.currentIndent;
            this.EnsureLeftCurly();

            do
            {
                if (this.currentToken == LexerToken.RightCurly || this.PeekToken() == LexerToken.RightCurly)
                {
                    while (startingIndent != this.currentIndent && this.PeekToken() == LexerToken.RightCurly && !this.eof)
                        ;
                    if (startingIndent == this.currentIndent)
                        break;
                }

                act();
            } while (!this.eof);
        }

        private void Init(Stream data, Action<ParadoxParser, string> action)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (action == null)
                throw new ArgumentNullException("parseStrategy");

            this.stream = data;
            this.InnerParse(action);
        }

        private void Init(string filePath, Action<ParadoxParser, string> action)
        {
            if (action == null)
                throw new ArgumentNullException("parseStrategy");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            using (this.stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                this.InnerParse(action);
            }
        }

        private void InnerParse(Action<ParadoxParser, string> tokenCallback)
        {
            using (this.reader = new StreamReader(this.stream, Encoding.GetEncoding(Globals.WindowsCodePage), false, BufferSize))
            {
                LexerToken pt;
                do
                {
                    while ((((pt = this.PeekToken()) == LexerToken.Untyped) || pt == LexerToken.Quote) && !this.eof)
                        tokenCallback(this, this.ReadString());
                } while (!this.eof);
            }
        }

        /// <summary>
        /// Sets the current token to the token associated with the parameter
        /// </summary>
        /// <param name="c">Byte that will be evaluated for equivalent token</param>
        /// <returns>Current token</returns>
        private LexerToken SetcurrentToken(char c)
        {
            this.currentToken = GetToken(c);
            if (this.currentToken == LexerToken.LeftCurly)
                this.currentIndent++;
            else if (this.currentToken == LexerToken.RightCurly)
                this.currentIndent--;

            return this.currentToken;
        }

        /// <summary>
        /// Advances the parser to the next significant token, skipping whitespace and
        /// comments.  If a left or right curly is encountered, the current indent is
        /// adjusted accordingly.
        /// </summary>
        /// <returns>The significant token encountered</returns>
        private LexerToken GetNextToken()
        {
            if (this.nextToken != null)
            {
                LexerToken temp = this.nextToken.Value;
                this.nextToken = null;
                return temp;
            }

            while (IsSpace(this.currentByte = this.ReadNext()) && !this.eof)
                ;

            if (this.SetcurrentToken(this.currentByte) == LexerToken.Comment)
            {
                while ((this.currentByte = this.ReadNext()) != '\n' && !this.eof)
                    ;
                return this.GetNextToken();
            }

            return this.currentToken;
        }

        /// <summary>
        /// Retrieves the next token, so that a subsequent call to <see cref="GetNextToken"/>
        /// will return the same token.  If multiple peekTokens are invoked then it is the last
        /// token encountered that <see cref="GetNextToken"/> will also return.
        /// <para/>
        /// <remarks>
        ///     This function is a misnomer in the traditional sense the peek does not affect the underlying
        ///     stream.  Though this function is prefixed with "peek", it advances the underlying stream.
        /// </remarks>
        /// </summary>
        /// <returns>The next token in the stream</returns>
        private LexerToken PeekToken()
        {
            this.nextToken = null;
            this.nextToken = this.GetNextToken();
            return this.nextToken.Value;
        }

        /// <summary>
        /// Retrieves the next char in the buffer, reading from the
        /// stream if necessary.  Since this is a raw char, if a number
        /// is expected, better to use <see cref="ReadInt32"/>
        /// </summary>
        /// <returns>The next raw byte in the buffer or 0 if the end of the file was reached</returns>
        private char ReadNext()
        {
            if (this.currentPosition == this.bufferSize)
            {
                if (!this.eof)
                    this.bufferSize = this.reader.Read(this.buffer, 0, BufferSize);

                this.currentPosition = 0;

                if (this.bufferSize == 0)
                {
                    this.eof = true;
                    return '\0';
                }
            }

            return this.buffer[this.currentPosition++];
        }
    }
}
