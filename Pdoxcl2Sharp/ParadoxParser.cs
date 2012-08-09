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
        private const int BUFFER_SIZE = 0x8000;

        private const NumberStyles SignedFloatingStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        public static bool IsSpace(byte c)
        {
            return c == SPACE || (c >= HORIZONTAL_TAB && c <= CARRIAGE_RETURN);
        }

        private static LexerToken getToken(byte c)
        {
            switch (c)
            {
                case EQUALS:
                    return LexerToken.Equals;
                case QUOTE:
                    return LexerToken.Quote;
                case LEFT_CURLY:
                    return LexerToken.LeftCurly;
                case RIGHT_CURLY:
                    return LexerToken.RightCurly;
                case LEFTPARANTHESIS:
                    return LexerToken.LeftParanthesis;
                case RIGHTPARANTHESIS:
                    return LexerToken.RightParanthesis;
                case COMMENT:
                case EXCLAMATION:
                case COMMA:
                    return LexerToken.Comment;
                default:
                    return LexerToken.Untyped;
            }
        }

        private int currentIndent;
        private LexerToken currentToken;
        private byte currentByte;
        private int currentPosition;
        private int bufferSize;
        private byte[] buffer = new byte[BUFFER_SIZE];
        private StringBuilder stringBuffer = new StringBuilder(MAX_TOKEN_SIZE);
        private Stream stream;

        private bool eof = false;

        public string CurrentString { get; private set; }


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

        public IParadoxFile Parse(IParadoxFile file)
        {
            parse(file.TokenCallback, currentIndent);
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

        private void parse(Action<ParadoxParser, string> tokenCallback, int stopIndent)
        {
            do
            {
                string currentLine = ReadString();

                if (currentLine != null)
                    tokenCallback(this, currentLine);
            } while (!eof && currentIndent < stopIndent);
        }

        private LexerToken getNextToken()
        {
            while (IsSpace(currentByte = readByte()) && !eof)
                ;

            currentToken = getToken(currentByte);

            switch (currentToken)
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
        /// Transfers the string buffer to the current string.
        /// Clears the buffer for the next round of reading
        /// </summary>
        /// <returns>The string contained inside the buffer</returns>
        private string saveBufferThenClear()
        {
            CurrentString = stringBuffer.ToString();
            stringBuffer.Clear();
            return CurrentString;
        }

        /// <summary>
        /// Retrieves the next byte in the buffer, reading from the
        /// stream if necessary.  Since this is a raw byte, if a number
        /// is expected, better to use <see cref="ReadInt32"/>
        /// </summary>
        /// <returns>The next raw byte in the buffer</returns>
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


        public string ReadString()
        {
            if (eof)
                return null;

            switch (getNextToken())
            {
                case LexerToken.Quote:
                    while ((currentByte = readByte()) != QUOTE && !eof)
                        stringBuffer.Append((char)currentByte);

                    return saveBufferThenClear();
                case LexerToken.Untyped:
                    do
                    {
                        stringBuffer.Append((char)currentByte);
                    } while (!IsSpace(currentByte = readByte()) && getToken(currentByte) == LexerToken.Untyped && !eof);

                    return saveBufferThenClear();
                default:
                    return CurrentString = ReadString();
            }
        }

        public int ReadInt32()
        {
            int result = 0;
            bool negative = false;

            while ((IsSpace(currentByte = readByte()) || getToken(currentByte) != LexerToken.Untyped) && !eof)
                ;

            if (eof)
                return 0;

            do
            {
                if (currentByte >= 0x30 && currentByte <= 0x39)
                {
                    result = 10 * result + (currentByte - 0x30);
                }
                else if (currentByte == 0x2D)
                {
                    //TODO: Only valid if there haven't been any numbers parsed
                    negative = true;
                }
                //TODO: If another character has been encountered throw an error
            } while (!IsSpace(currentByte = readByte()) && getToken(currentByte) == LexerToken.Untyped && !eof);

            return (negative) ? -result : result;
        }

        public double ReadDouble()
        {
            double result;
            if (double.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new Exception();
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
            throw new Exception();
        }

        /// <summary>
        /// Advances the parser through the left bracket ('{') and then invokes the action.
        /// It is assumed that the action will consume only what is contained in the brackets.
        /// </summary>
        /// <param name="action">Action that will be invoked after the parser has advanced through the leading bracket</param>
        public void ReadInsideBrackets(Action<ParadoxParser> action)
        {
            int startingIndent = currentIndent;

            //Advance through the '{'
            if (getNextToken() != LexerToken.LeftCurly)
                throw new InvalidOperationException("When reading inside brackets the first token must be a left curly");

            action(this);

            switch (currentIndent.CompareTo(startingIndent))
            {
                case -1:
                    throw new InvalidOperationException("Invoked action parsed further than the closing bracket");
                case 0:
                    return;
                case 1:
                    //Advance until the closing curly brace
                    while (getNextToken() != LexerToken.RightCurly && startingIndent == currentIndent && !eof)
                        ;
                    break;
            }
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
    }
}
