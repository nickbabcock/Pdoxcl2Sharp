using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace Pdoxcl2Sharp
{
    enum LexerToken
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
        // White space constants
        private const byte SPACE = 0x20;
        private const byte HORIZONTAL_TAB = 0x09;
        private const byte NEWLINE = 0x0A;
        private const byte VERTICAL_TAB = 0x0B;
        private const byte FEED = 0x0C;
        private const byte CARRIAGE_RETURN = 0x0D;

        //Single character contants
        private const byte EQUALS = 0x3D;
        private const byte QUOTE = 0x22;
        private const byte RIGHT_CURLY = 0x7D;
        private const byte LEFT_CURLY = 0x7B;
        private const byte COMMENT = 0x23;
        private const byte SEMI_COLON = 0x3B;
        private const byte LEFTPARANTHESIS = 0x28;
        private const byte RIGHTPARANTHESIS = 0x29;
        private const byte EXCLAMATION = 0x21;
        private const byte COMMA = 0x2C;

        private const int MAX_TOKEN_SIZE = 256;
        private const int BUFFER_SIZE = 0x8000; //32KB buffer

        private const NumberStyles SignedFloatingStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;


        /// <summary>
        /// Checks to see whether a given byte is considered a whitespace: space, horizontal tab,
        /// newline, vertical tab, feed, or carriage return
        /// </summary>
        /// <param name="c">The byte that will be tested if it is whitespace</param>
        /// <returns>True if the byte is whitespace</returns>
        public static bool IsSpace(byte c)
        {
            return c == SPACE || (c >= HORIZONTAL_TAB && c <= CARRIAGE_RETURN);
        }

        /// <summary>
        /// Sets the current token to the token associated with the parameter
        /// </summary>
        /// <param name="c">Byte that will be evaluated for equivalent token</param>
        /// <returns>Current token</returns>
        private LexerToken setCurrentToken(byte c)
        {
            return currentToken = getToken(c);
        }

        /// <summary>
        /// Returns the associated LexerToken with a given byte.  Compares the byte with 
        /// known ASCII values of the tokens
        /// </summary>
        /// <param name="c"></param>
        /// <returns>LexerToken</returns>
        private static LexerToken getToken(byte c)
        {
            switch (c)
            {
                case EQUALS:
                    return  LexerToken.Equals;
                case QUOTE:
                    return  LexerToken.Quote;
                case LEFT_CURLY:
                    return  LexerToken.LeftCurly;
                case RIGHT_CURLY:
                    return  LexerToken.RightCurly;
                case LEFTPARANTHESIS:
                    return  LexerToken.LeftParanthesis;
                case RIGHTPARANTHESIS:
                    return  LexerToken.RightParanthesis;
                case COMMENT:
                case EXCLAMATION:
                    return  LexerToken.Comment;
                case COMMA:
                    return  LexerToken.Comma;
                default:
                    return  LexerToken.Untyped;
            }
        }

        private int currentIndent;
        private LexerToken currentToken;
        private LexerToken? nextToken;
        private byte currentByte;
        private int currentPosition;
        private int bufferSize;
        private byte[] buffer = new byte[BUFFER_SIZE];
        private StringBuilder stringBuffer = new StringBuilder(MAX_TOKEN_SIZE);
        private Stream stream;

        private bool eof = false;

        private string currentString;


        public ParadoxParser(byte[] data, Action<ParadoxParser, string> parseStrategy)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (parseStrategy == null)
                throw new ArgumentNullException("parseStrategy");

            using (stream = new MemoryStream(data))
            {
                parse(parseStrategy);
            }
        }

        public ParadoxParser(IParadoxFile file, string filePath)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            using (stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                parse(file.TokenCallback);
            }
        }

        public ParadoxParser(string filePath, Action<ParadoxParser, string> parseStrategy)
        {
            if (parseStrategy == null)
                throw new ArgumentNullException("parseStrategy");

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            using (stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                parse(parseStrategy);
            }
        }

        /// <summary>
        /// Used to parse a set of tokens that are contained within curly brackets.
        /// For example, if a file contains a set of countries and each country was an <see cref="IParadoxFile"/>,
        /// the file would invoke this method whenever it found a new country.
        /// </summary>
        /// <param name="innerStructure">Defines how to parse the inner set of tokens.</param>
        /// <returns>The passed in parameter newly parsed</returns>
        public T Parse<T>(T file) where T : class, IParadoxFile
        {
            int stopIndent = currentIndent;
            do
            {
                string currentLine = ReadString();

                if (currentLine != null)
                    file.TokenCallback(this, currentLine);
            } while (!eof && currentIndent < stopIndent);
            return file;
        }

        private void parse(Action<ParadoxParser, string> tokenCallback)
        {
            do
            {
                string currentLine = ReadString();

                if (currentLine != null)
                    tokenCallback(this, currentLine);
            } while (!eof);
        }


        /// <summary>
        /// Advances the parser to the next significant token, skipping whitespace and
        /// comments.  If a left or right curly is encountered, the current indent is
        /// adjusted accordingly.
        /// </summary>
        /// <returns>The significant token encountered</returns>
        private LexerToken getNextToken()
        {
            if (nextToken != null)
            {
                LexerToken temp = nextToken.Value;
                nextToken = null;
                return temp;
            }

            while (IsSpace(currentByte = readByte()) && !eof)
                ;

            switch (setCurrentToken(currentByte))
            {
                case LexerToken.Comment:
                    while ((currentByte = readByte()) != NEWLINE && !eof)
                        ;
                    return getNextToken();
                case LexerToken.LeftCurly:
                    currentIndent++;
                    return LexerToken.LeftCurly;
                case LexerToken.RightCurly:
                    currentIndent--;
                    return LexerToken.RightCurly;
                default:
                    return currentToken;
            }
        }

        /// <summary>
        /// Retrieves the next token, so that a subsequent call to <see cref="getNextToken"/>
        /// will return the same token.  If multiple peekTokens are invoked then it is the last
        /// token encountered that <see cref="getNextToken"/> will also return.
        /// 
        /// <remarks>
        ///     This function is a misnomer in the traditional sense the peek does not affect the underlying
        ///     stream.  Though this function is prefixed with "peek", it advances the underlying stream.
        /// </remarks>
        /// </summary>
        /// <returns>The next token in the stream</returns>
        private LexerToken peekToken()
        {
            nextToken = null;
            nextToken = getNextToken();
            return nextToken.Value;
        }

        /// <summary>
        /// Transfers the string buffer to the current string.
        /// Clears the buffer for the next round of reading
        /// </summary>
        /// <returns>The string contained inside the buffer</returns>
        private string saveBufferThenClear()
        {
            currentString = stringBuffer.ToString();
            stringBuffer.Clear();
            return currentString;
        }

        /// <summary>
        /// Retrieves the next byte in the buffer, reading from the
        /// stream if necessary.  Since this is a raw byte, if a number
        /// is expected, better to use <see cref="ReadInt32"/>
        /// </summary>
        /// <returns>The next raw byte in the buffer or 0 if the end of the file was reached</returns>
        private byte readByte()
        {
            if (currentPosition == bufferSize)
            {
                if (!eof)
                    bufferSize = stream.Read(buffer, 0, BUFFER_SIZE);

                currentPosition = 0;

                if (bufferSize == 0)
                {
                    eof = true;
                    return 0;
                }
            }

            return buffer[currentPosition++];
        }

        /// <summary>
        /// Returns, in string form, the bytes between two tokens, unless the a quote is 
        /// encountered, which then all the bytes between two enclosing quotes will be returned
        /// without the quotes in the return value
        /// </summary>
        /// <returns>Returns null if the end of file encountered</returns>
        public string ReadString()
        {
            if (eof)
                return null;

            getNextToken();

            if (eof)
                return null;

            switch (currentToken)
            {
                case LexerToken.Quote:
                    while ((currentByte = readByte()) != QUOTE && !eof)
                        stringBuffer.Append((char)currentByte);

                    return saveBufferThenClear();
                case LexerToken.Untyped:
                    do
                    {
                        stringBuffer.Append((char)currentByte);
                    } while (!IsSpace(currentByte = readByte()) && setCurrentToken(currentByte) == LexerToken.Untyped && !eof);

                    return saveBufferThenClear();
                default:
                    return currentString = ReadString();
            }
        }

        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as an int32
        /// </summary>
        /// <returns>int32 associated with the next series of bytes in the parser</returns>
        public int ReadInt32()
        {
            int result = 0;
            bool negative = false;

            while (getNextToken() != LexerToken.Untyped && !eof)
                ;

            if (eof)
                return 0;

            do
            {
                if (currentByte >= 0x30 && currentByte <= 0x39)
                    result = 10 * result + (currentByte - 0x30);
                else if (currentByte == 0x2D)
                {
                    //TODO: Only valid if there haven't been any numbers parsed
                    negative = true;
                }
                //TODO: If another character has been encountered throw an error
            } while (!IsSpace(currentByte = readByte()) && setCurrentToken(currentByte) == LexerToken.Untyped && !eof);

            return (negative) ? -result : result;
        }

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a short
        /// </summary>
        /// <returns>Short associated with the next series of bytes in the parser</returns>
        public short ReadInt16() { return (short)ReadInt32(); }
        

        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a sbyte
        /// </summary>
        /// <returns>Sbyte associated with the next series of bytes in the parser</returns>
        public sbyte ReadSByte() { return (sbyte)ReadInt32(); }


        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as an uint.
        /// </summary>
        /// <returns>uint associated with the next series of bytes in the parser</returns>
        public uint ReadUInt32()
        {
            uint result = 0;

            while (getNextToken() != LexerToken.Untyped && !eof)
                ;

            if (eof)
                return 0;

            do
            {
                result = (uint)(10 * result + (currentByte - 0x30));
            } while (!IsSpace(currentByte = readByte()) && setCurrentToken(currentByte) == LexerToken.Untyped && !eof);
            return result;
        }


        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as an ushort.
        /// </summary>
        /// <returns>ushort associated with the next series of bytes in the parser</returns>
        public ushort ReadUInt16() { return (ushort)ReadUInt32(); }


        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as a byte.
        /// </summary>
        /// <returns>Byte associated with the next series of bytes in the parser</returns>
        public byte ReadByte() { return (byte)ReadUInt32(); }


        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as a double.
        /// </summary>
        /// <returns>Double associated with the next series of bytes in the parser</returns>
        public double ReadDouble()
        {
            double result;
            if (double.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct Double", currentString));
        }


        /// <summary>
        /// Advances the parser and inteprets whatever was encountered as a Float.
        /// </summary>
        /// <returns>Float associated with the next series of bytes in the parser</returns>
        public float ReadFloat()
        {
            float result;
            if (float.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct Float", currentString));
        }


        /// <summary>
        /// Advances the parser and interprets whatever was encountered as a DateTime
        /// </summary>
        /// <returns>System.DateTime associated with next series of bytes in the parser</returns>
        public DateTime ReadDateTime()
        {
            DateTime result;
            if (TryParseDate(ReadString(), out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct DateTime", currentString));
        }


        /// <summary>
        /// Reads the data between brackets as ints
        /// </summary>
        /// <returns>A list of the data interpreted as ints</returns>
        public IList<int> ReadIntList()
        {
            List<int> result = new List<int>();
            do
            {
                while (peekToken() != LexerToken.Untyped && !eof)
                    ;

                if (eof)
                    return result;

                result.Add(ReadInt32());
            } while (currentToken != LexerToken.RightCurly && !eof);

            return result;
        }


        /// <summary>
        /// Reads the data between brackets as doubles
        /// </summary>
        /// <returns>A list of the data interpreted as doubles</returns>
        public IList<double> ReadDoubleList()
        {
            List<double> result = new List<double>();
            do
            {
                if (!String.IsNullOrEmpty(ReadString()) && !eof)
                    result.Add(double.Parse(currentString, SignedFloatingStyle, CultureInfo.InvariantCulture));
            } while (peekToken() != LexerToken.RightCurly && !eof);
            return result;
        }


        /// <summary>
        /// Reads the data between brackets as strings
        /// </summary>
        /// <returns>A list of the data interpreted as strings</returns>
        public IList<string> ReadStringList()
        {
            List<string> result = new List<string>();
            while (peekToken() != LexerToken.RightCurly && !eof)
            {
                if (!String.IsNullOrEmpty(ReadString()))
                    result.Add(currentString);
            }
            return result;
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
            
            int startingIndent = currentIndent;
            IDictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            ensureLeftCurly();
            while (peekToken() != LexerToken.RightCurly && !eof)
            {
                result.Add(keyFunc(this), valueFunc(this));
            }
            return result;
        }


        /// <summary>
        /// Advances the parser through the left bracket ('{') and then invokes the action.
        /// It is assumed that the action will consume only what is contained in the brackets.
        /// This function is useful when the number of data within brackets is known but may be of various types.
        /// </summary>
        /// <param name="action">An action that the parser will perform to extract data inside the curly brackets</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void ReadInsideBrackets(Action<ParadoxParser> action)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action for reading bracket content must not be null");

            int startingIndent = currentIndent;

            ensureLeftCurly();
            action(this);

            switch (currentIndent.CompareTo(startingIndent))
            {
                case -1:
                    throw new InvalidOperationException("Invoked action parsed further than the closing bracket");
                case 0:
                    return;
                case 1:
                    //Advance until the closing curly brace
                    while (getNextToken() != LexerToken.RightCurly && startingIndent != currentIndent && !eof)
                        ;
                    break;
            }
        }


        /// <summary>
        /// Checks the next token in the stream in an attempt to locate a left curly.  If the token encountered is 
        /// an equality symbol, it will read the next token and see if that is a left curly, e.g. x = { y }.
        /// If the initial token isn't an equality symbol or a left curly, or if the initial token is an equality symbol
        /// but the subsequent token isn't a left curly, then an exception is thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void ensureLeftCurly()
        {
            currentToken = getNextToken();
            if (currentToken == LexerToken.Equals)
                currentToken = getNextToken();

            if (currentToken != LexerToken.LeftCurly)
                throw new InvalidOperationException("When reading inside brackets the first token must be a left curly");
        }


	    /// <summary>
        /// Returns the equivalent System.DateTime as the input string.  Simple wrapper around
        /// DateTime.TryParseExact with specified format and invariant info.  This function is
        /// designed to work with tokens or strings created by <see cref="ReadString"/> and as
        /// such, doesn't allow whitespace.
        /// </summary>
        /// <param name="dateTime">A string containing the date to parse.</param>
        /// <param name="result">Contains the equivalent System.DateTime as the input parameter</param>
        /// <returns>True if the conversion was successful</returns>
        public static bool TryParseDate(string dateTime, out DateTime result)
        { 
            return DateTime.TryParseExact(dateTime, "yyyy.M.d", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result);
        }

        public static void Parse(byte[] data, Action<ParadoxParser, string> parseStrategy)
        {
            ParadoxParser p = new ParadoxParser(data, parseStrategy);
        }

        public static void Parse(IParadoxFile file, string filePath)
        {
            ParadoxParser p = new ParadoxParser(file, filePath);
        }

        public static void Parse(string filePath, Action<ParadoxParser, string> parseStrategy)
        {
            ParadoxParser p = new ParadoxParser(filePath, parseStrategy);
        }

    }
}
