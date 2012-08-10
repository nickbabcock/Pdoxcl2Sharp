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
        private byte[] buffer = new byte[Globals.BUFFER_SIZE];
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

        public T Parse<T>(T file) where T : class, IParadoxFile
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

        private string saveBufferThenClear()
        {
            CurrentString = stringBuffer.ToString();
            stringBuffer.Clear();
            return CurrentString;
        }

        private byte readByte()
        {
            if (currentPosition == bufferSize)
            {
                if (!eof)
                    bufferSize = stream.Read(buffer, 0, Globals.BUFFER_SIZE);

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
                    result = 10 * result + (currentByte - 0x30);
                else if (currentByte == 0x2D)
                {
                    //TODO: Only valid if there haven't been any numbers parsed
                    negative = true;
                }
                //TODO: If another character has been encountered throw an error
            } while (!IsSpace(currentByte = readByte()) && getToken(currentByte) == LexerToken.Untyped && !eof);

            return (negative) ? -result : result;
        }

        public short ReadInt16() { return (short)ReadInt32(); }
        public sbyte ReadSByte() { return (sbyte)ReadInt32(); }

        public uint ReadUInt32()
        {
            uint result = 0;

            while ((IsSpace(currentByte = readByte()) || getToken(currentByte) != LexerToken.Untyped) && !eof)
                ;

            if (eof)
                return 0;

            do
            {
                result = (uint)(10 * result + (currentByte - 0x30));
            } while (!IsSpace(currentByte = readByte()) && getToken(currentByte) == LexerToken.Untyped && !eof);
            return result;
        }
        public ushort ReadUInt16() { return (ushort)ReadUInt32(); }
        public byte ReadByte() { return (byte)ReadUInt32(); }

        public double ReadDouble()
        {
            double result;
            if (double.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new Exception();
        }

        public float ReadFloat()
        {
            float result;
            if (float.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new Exception();
        }

        public DateTime ReadDateTime()
        {
            DateTime result;
            if (TryParseDate(ReadString(), out result))
                return result;
            throw new Exception();
        }

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

        public static bool TryParseDate(string dateTime, out DateTime result)
        {
            return DateTime.TryParseExact(dateTime, "yyyy.M.d", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result);
        }

        public static void Parse(byte[] data, Action<ParadoxParser, string> parseStrategy, int bufferSize = Globals.BUFFER_SIZE)
        {
            ParadoxParser p = new ParadoxParser(data, parseStrategy);
        }

        public static void Parse(IParadoxFile file, string filePath, int bufferSize = Globals.BUFFER_SIZE)
        {
            ParadoxParser p = new ParadoxParser(file, filePath);
        }

        public static void Parse(string filePath, Action<ParadoxParser, string> parseStrategy, int bufferSize = Globals.BUFFER_SIZE)
        {
            ParadoxParser p = new ParadoxParser(filePath, parseStrategy);
        }
    }
}
