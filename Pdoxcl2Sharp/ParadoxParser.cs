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

        public static bool IsSpace(byte c)
        {
            return c == SPACE || (c >= HORIZONTAL_TAB && c <= CARRIAGE_RETURN);
        }

        private LexerToken setCurrentToken(byte c)
        {
            return currentToken = getToken(c);
        }
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

        private LexerToken peekToken()
        {
            nextToken = null;
            nextToken = getNextToken();
            return nextToken.Value;
        }

        private string saveBufferThenClear()
        {
            currentString = stringBuffer.ToString();
            stringBuffer.Clear();
            return currentString;
        }
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

        public short ReadInt16() { return (short)ReadInt32(); }
        public sbyte ReadSByte() { return (sbyte)ReadInt32(); }

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

        public ushort ReadUInt16() { return (ushort)ReadUInt32(); }
        public byte ReadByte() { return (byte)ReadUInt32(); }

        public double ReadDouble()
        {
            double result;
            if (double.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct Double", currentString));
        }

        public float ReadFloat()
        {
            float result;
            if (float.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct Float", currentString));
        }

        public DateTime ReadDateTime()
        {
            DateTime result;
            if (TryParseDate(ReadString(), out result))
                return result;
            throw new FormatException(String.Format(CultureInfo.InvariantCulture, "{0} is not a correct DateTime", currentString));
        }

        public IList<int> ReadIntList()
        {
            List<int> result = new List<int>();
            advanceThroughLeftCurly();
            while (peekToken() != LexerToken.RightCurly && !eof)
                result.Add(ReadInt32());
            return result;
        }


        public IList<double> ReadDoubleList()
        {
            List<double> result = new List<double>();
            advanceThroughLeftCurly();
            while (peekToken() != LexerToken.RightCurly && !eof)
                result.Add(double.Parse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture));
            return result;
        }

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

        public IDictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Func<ParadoxParser, TKey> keyFunc, Func<ParadoxParser, TValue> valueFunc)
        {
            if (keyFunc == null)
                throw new ArgumentNullException("keyFunc", "Function for extracting keys must not be null");
            if (valueFunc == null)
                throw new ArgumentNullException("valueFunc", "Function for extracting values must not be null");
            
            int startingIndent = currentIndent;
            IDictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            advanceThroughLeftCurly();
            while (peekToken() != LexerToken.RightCurly && !eof)
            {
                result.Add(keyFunc(this), valueFunc(this));
            }
            return result;
        }
        public void ReadInsideBrackets(Action<ParadoxParser> action)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action for reading bracket content must not be null");

            int startingIndent = currentIndent;

            advanceThroughLeftCurly();
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

        private void advanceThroughLeftCurly()
        {
            if ((currentToken = getNextToken()) == LexerToken.Equals)
                currentToken = getNextToken();

            if (currentToken != LexerToken.LeftCurly)
                throw new InvalidOperationException("When reading inside brackets the first token must be a left curly");
        }

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
