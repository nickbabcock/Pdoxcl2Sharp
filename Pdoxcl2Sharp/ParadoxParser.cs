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

        private const NumberStyles SignedFloatingStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        /// <summary>
        /// The number of bytes that is desired to read at a time from the
        /// stream. 64 KB was chosen due to the paper: "Sequential File
        /// Programming Patterns and Performance with .NET" stating "A rule of
        /// thumb is that the minimum recommended transfer size is 64 KB and
        /// that bigger transfers are generally better." A bigger buffer wasn't
        /// chosen because performance boosts dropped dramatically after 64 KB.
        /// </summary>
        private const int MaxByteBuffer = 1024 * 64;

        /// <summary>
        /// The number of characters that will be processed at a time. This is
        /// capped at encoding level as there could be a possibility of the
        /// char buffer being bigger than the byte buffer (byte-wise).
        /// </summary>
        private static readonly int BufferSize = Globals.ParadoxEncoding.GetMaxCharCount(MaxByteBuffer);

        private int currentIndent;
        private LexerToken currentToken;
        private Queue<LexerToken> nextTokens = new Queue<LexerToken>();
        private char currentChar;
        private int currentPosition;
        private int bufferSize;
        private char[] buffer = new char[BufferSize];
        private char[] stringBuffer = new char[MaxTokenSize];
        private int stringBufferCount = 0;
        private TextReader reader;
        private bool eof = false;
        private bool tagIsBracketed = false;
        private string currentString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxParser"/> class.
        /// Parses a stream and executes the parsing strategy when an unknown token is encountered.
        /// </summary>
        /// <param name="data">Stream to be parsed</param>
        /// <exception cref="ArgumentNullException">If <paramref name="data"/> is null</exception>
        private ParadoxParser(TextReader data)
        {
            this.reader = data;
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
        /// Gets the last string read by the parser
        /// </summary>
        public string CurrentString
        {
            get { return this.currentString; }
        }

        /// <summary>
        /// Gets a value indicating whether the parser is at the end of the stream
        /// </summary>
        public bool EndOfStream
        {
            get { return this.eof; }
        }

        /// <summary>
        /// Converts the specified string representation of a date and time from
        /// Paradox's format of (year).(month).(day) and optionally (hour) to
        /// its <see cref="DateTime"/> equivalent, and returns a value that
        /// indicates whether the conversion succeeded. 
        /// </summary>
        /// <param name="dateTime">A string containing the date to parse.</param>
        /// <param name="result">
        /// When this method returns, contains the <see cref="DateTime"/> equivalent
        /// to the date and time contained in string parameter, if the conversion succeeded,
        /// or MinValue if the conversion failed.
        /// </param>
        /// <returns>True if the conversion was successful</returns>
        public static bool TryParseDate(string dateTime, out DateTime result)
        {
            result = DateTime.MinValue;
            string[] splitted = dateTime.Split('.');
            int?[] date = new int?[6];
            int t;
            for (int i = 0; i < splitted.Length; i++)
            {
                if (!int.TryParse(splitted[i], NumberStyles.None, CultureInfo.InvariantCulture, out t))
                    return false;
                date[i] = t;
            }

            int y = date[0] ?? 1, m = date[1] ?? 1, d = date[2] ?? 1,
                hh = date[3] ?? 0, mm = date[4] ?? 0, s = date[5] ?? 0;
            if ((y < 1 || y > 9999) || (m < 1 || m > 12) || 
                (d < 1 || d > DateTime.DaysInMonth(y, m)) || (hh < 0 || hh > 23) ||
                (mm < 0 || mm > 59) || (s < 0 || s > 59))
                return false;
            result = new DateTime(y, m, d, hh, mm, s);
            return true;
        }

        /// <summary>
        /// Checks to see whether a given character is considered whitespace.
        /// </summary>
        /// <param name="c">The character to evaluate</param>
        /// <returns>True if <paramref name="c"/> is whitespace</returns>
        public static bool IsSpace(char c)
        {
            return c == ' ' || (c >= '\t' && c <= '\r');
        }

        /// <summary>
        /// Given a stream, the function will deserialize it into a
        /// specific type. 
        /// </summary>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <param name="data">The stream to extract the object</param>
        /// <returns>The object deserialized from the stream</returns>
        public static T Deserialize<T>(Stream data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            using (var reader = new StreamReader(data, Globals.ParadoxEncoding, false, MaxByteBuffer))
            {
                FnPtr ptr = Deserializer.Parse(typeof(T));
                ParadoxParser parser = new ParadoxParser(reader);
                return (T)ptr(parser);
            }
        }

        /// <summary>
        /// Parses a given stream and applies an action to each token found
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="parseStrategy">Action to be performed on found tokens</param>
        public static void Parse(Stream data, Action<ParadoxParser, string> parseStrategy)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            using (var reader = new StreamReader(data, Globals.ParadoxEncoding, false, MaxByteBuffer))
            {
                ParadoxParser parser = new ParadoxParser(reader);
                while (!parser.EndOfStream)
                {
                    string val = parser.ReadString();

                    // if val is the null string then nothing of importance
                    // was found between the last token and the end of the stream 
                    if (val != "\0")
                        parseStrategy(parser, val);
                }
            }
        }

        /// <summary>
        /// Parses a given stream with a defined structure
        /// </summary>
        /// <param name="data">Stream to parse</param>
        /// <param name="entity">Defines how to parse the stream</param>
        /// <typeparam name="T">Type of the structure to be parsed</typeparam>
        /// <returns><paramref name="entity"/> in a fluent interface</returns>
        public static T Parse<T>(Stream data, T entity) where T : IParadoxRead
        {
            Parse(data, entity.TokenCallback);
            return entity;
        }

        /// <summary>
        /// Reads everything between same level brackets and parses it based on the 
        /// <paramref name="innerStructure"/>
        /// </summary>
        /// <param name="innerStructure">Defines how to parse the inner set of tokens.</param>
        /// <typeparam name="T">Type of the structure to parse</typeparam>
        /// <returns><paramref name="innerStructure"/> in a fluent interface</returns>
        public T Parse<T>(T innerStructure) where T : class, IParadoxRead
        {
            Parse(innerStructure.TokenCallback);
            return innerStructure;
        }

        /// <summary>
        /// Reads everything between same level brackets and applies the action to each token
        /// </summary>
        /// <param name="action">The action to be performed on each token</param>
        public void Parse(Action<ParadoxParser, string> action)
        {
            this.DoWhileBracket(() => action(this, this.ReadString()));
        }

        /// <summary>
        /// Reads a string from the current stream. String will be strictly composed of 
        /// <see cref="LexerToken.Untyped"/>. If quotes are encountered in the stream then the return value
        /// will be the string that is contained within the quotes (the quotes are stripped)
        /// <see cref="CurrentString"/> is set to the return value.
        /// </summary>
        /// <returns>The string being read</returns>
        public string ReadString()
        {
            this.currentToken = this.GetNextToken();
            if (this.currentToken == LexerToken.Untyped)
            {
                do
                {
                    this.stringBuffer[this.stringBufferCount++] = this.currentChar;
                } while (!IsSpace(this.currentChar = this.ReadNext()) && 
                    this.SetcurrentToken(this.currentChar) == LexerToken.Untyped && !this.eof);
            }
            else if (this.currentToken == LexerToken.Quote)
            {
                while ((this.currentChar = this.ReadNext()) != '"' && !this.eof)
                    this.stringBuffer[this.stringBufferCount++] = this.currentChar;
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
        /// Reads a 4-byte signed integer from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A 4-byte signed integer read from the current stream</returns>
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
                if (this.currentChar >= '0' && this.currentChar <= '9')
                    result = (10 * result) + (this.currentChar - '0');
                else if (this.currentChar == '-')
                    negative = true;
            } while (!IsSpace(this.currentChar = this.ReadNext()) &&
                this.SetcurrentToken(this.currentChar) == LexerToken.Untyped && !this.eof);

            return negative ? -result : result;
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A 2-byte signed integer read from the current stream</returns>
        public short ReadInt16() 
        { 
            return (short)this.ReadInt32();
        }
        
        /// <summary>
        /// Reads a signed byte from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A signed byte read from the current stream</returns>
        public sbyte ReadSByte() 
        { 
            return (sbyte)this.ReadInt32();
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A 4-byte unsigned integer read from the current stream</returns>
        public uint ReadUInt32()
        {
            uint result = 0;

            while (this.GetNextToken() != LexerToken.Untyped && !this.eof)
                ;

            if (this.eof)
                return 0;

            do
            {
                result = (uint)((10 * result) + (this.currentChar - '0'));
            } while (!IsSpace(this.currentChar = this.ReadNext()) &&
                this.SetcurrentToken(this.currentChar) == LexerToken.Untyped && !this.eof);
            return result;
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A 2-byte unsigned integer read from the current stream</returns>
        public ushort ReadUInt16() 
        { 
            return (ushort)this.ReadUInt32();
        }

        /// <summary>
        /// Reads a byte from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the number
        /// </summary>
        /// <returns>A byte read from the current stream</returns>
        public byte ReadByte() 
        { 
            return (byte)this.ReadUInt32();
        }

        /// <summary>
        /// Reads a boolean from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation
        /// of the boolean
        /// </summary>
        /// <returns>A boolean read from the current stream</returns>
        public bool ReadBool()
        {
            return this.ReadString() == "yes";
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the floating point
        /// </summary>
        /// <returns>An 8-byte floating point value read from the current stream.</returns>
        public double ReadDouble()
        {
            double result;
            if (double.TryParse(this.ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(string.Format("{0} is not a correct Double", this.currentString));
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the floating point
        /// </summary>
        /// <returns>An 4-byte floating point value read from the current stream.</returns>
        public float ReadFloat()
        {
            float result;
            if (float.TryParse(this.ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(string.Format("{0} is not a correct Float", this.currentString));
        }

        /// <summary>
        /// Reads a <see cref="DateTime"/> from the current stream and advances the current position
        /// of the stream a variable amount depending on the string representation of the date and time.
        /// </summary>
        /// <returns><see cref="DateTime"/> read from the current stream</returns>
        public DateTime ReadDateTime()
        {
            DateTime result;
            if (TryParseDate(this.ReadString(), out result))
                return result;
            throw new FormatException(string.Format("{0} is not a correct DateTime", this.currentString));
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
        public IDictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            Func<ParadoxParser, TKey> keyFunc,
            Func<ParadoxParser, TValue> valueFunc)
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
        /// Parses the data between curly brackets in a manner dictated by the function parameter
        /// </summary>
        /// <typeparam name="T">Type that the data will be interpreted as</typeparam>
        /// <param name="func">Function that will extract the data</param>
        /// <returns>Data between curly brackets constructed as a list.</returns>
        internal IList<T> ReadList<T>(Func<T> func)
        {
            List<T> result = new List<T>();
            this.DoWhileBracket(() => result.Add(func()));
            return result;
        }

        /// <summary>
        /// Returns the <see cref="LexerToken"/> representation of the parameter
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
        /// Checks the current token, and if needed, reads next token in the
        /// stream in an attempt to locate a left curly.  If the token
        /// encountered is an equality symbol, it will read the next token and
        /// see if that is a left curly, e.g. x = { y }.  If the initial read
        /// token isn't an equality symbol or a left curly, or if the initial
        /// read token is an equality symbol but the subsequent token isn't a
        /// left curly, then an invalid operation exception is thrown.
        /// </summary>
        private void EnsureLeftCurly()
        {
			// Need to check tagIsBracketed and increment, because running NextIsBracket will prematurely count
			// the left curly as incremented and then decrement currentIndent back to where it was.
			if (this.currentToken == LexerToken.LeftCurly)
			{
				if (this.tagIsBracketed)
					currentIndent++;
				return;
			}

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
        /// a right curly.  Therefore, if the action doesn't advance the stream,
        /// the current token will never be a right curly and <see cref="PeekToken"/> will
        /// always be invoked. In short, <see cref="PeekToken"/> advances the stream, so
        /// an infinite loop is impossible
        /// </remarks>
        /// <param name="act">The action that will be repeatedly performed while there is data left in the brackets</param>
        private void DoWhileBracket(Action act)
        {
            int startingIndent = this.currentIndent;

            if (this.currentString != null)
                this.EnsureLeftCurly();

            do
            {
                if (this.currentToken == LexerToken.RightCurly 
                    || this.PeekToken() == LexerToken.RightCurly)
                {
                    while (startingIndent != this.currentIndent 
                        && this.PeekToken() == LexerToken.RightCurly && !this.eof)
                        ;
                    if (startingIndent == this.currentIndent)
                        break;
                }

                if (this.stringBufferCount != 0 || this.currentChar != '\0')
                    act();
            } while (!this.eof);
        }

        /// <summary>
        /// Sets the current token to the token associated with the parameter
        /// </summary>
        /// <param name="c">Char that will be evaluated for equivalent token</param>
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
            this.tagIsBracketed = false;
            
            if (this.nextTokens.Count != 0)
            {
                LexerToken temp = this.nextTokens.Dequeue();
                return temp;
            }

            while (IsSpace(this.currentChar = this.ReadNext()) && !this.eof)
                ;

            if (this.SetcurrentToken(this.currentChar) == LexerToken.Comment)
            {
                while ((this.currentChar = this.ReadNext()) != '\n' && !this.eof)
                    ;
                return this.GetNextToken();
            }

            return this.currentToken;
        }

        /// <summary>
        /// Retrieves the next token, so that a subsequent call to <see cref="GetNextToken"/>
        /// will return the same token.  If multiple peekTokens are invoked then it is the last
        /// token encountered that <see cref="GetNextToken"/> will also return.
        /// <remarks>
        ///     This function is a misnomer in the traditional sense the peek
        ///     does not affect the underlying stream.  Though this function is
        ///     prefixed with "peek", it advances the underlying stream.
        /// </remarks>
        /// </summary>
        /// <returns>The next token in the stream</returns>
        private LexerToken PeekToken()
        {
            if (this.nextTokens.Count > 0)
                this.nextTokens.Dequeue();
            this.nextTokens.Enqueue(this.GetNextToken());
            return this.nextTokens.Peek();
        }

        /// <summary>
        /// "Peeks" ahead of the current position to check if the previously read data
        /// is followed by a left bracket ('{'). Multiple calls without reading data will
        /// return the same value.
        /// </summary>
        /// <returns>Whether the current tag contains bracketed data.</returns>
        public bool NextIsBracketed()
        {
            if (this.tagIsBracketed)
                return this.tagIsBracketed;

            bool isBracketed = false;
			Queue<LexerToken> tempQueue = new Queue<LexerToken>();

            if (this.currentToken != LexerToken.LeftCurly)
            {
                LexerToken temp;
                do
                {
                    temp = this.GetNextToken();
                    tempQueue.Enqueue(temp);

                    if (temp == LexerToken.LeftCurly)
                    {
                        isBracketed = true;
						// Peeking for the next bracket will prematurely increment currentIndent.
	                    currentIndent--;
                        break;
                    }
                } while ((temp == LexerToken.Equals || temp == LexerToken.LeftCurly) && !this.eof);

				while (tempQueue.Count > 0)
					this.nextTokens.Enqueue( tempQueue.Dequeue() );
            }

            this.tagIsBracketed = isBracketed;
            return tagIsBracketed;
        }

        /// <summary>
        /// Retrieves the next char in the buffer, reading from the stream if necessary.  
        /// If the end of the stream was reached, the flag denoting it will be set.
        /// </summary>
        /// <returns>The next character in the buffer or '\0' if the end of the stream was reached</returns>
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
