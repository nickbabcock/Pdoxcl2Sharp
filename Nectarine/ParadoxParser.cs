using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace Nectarine
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

        //Single character tokens
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

        private const NumberStyles SignedFloatingStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        public static bool IsSpace(byte c)
        {
            return c == SPACE || (c >= HORIZONTAL_TAB && c <= CARRIAGE_RETURN);
        }

        private static LexerToken IsSingleCharTok(byte c)
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
        private int desiredBufferSize;
        private byte[] buffer;
        private StringBuilder stringBuffer;
        private Stream stream;

        private bool eof = false;

        public string CurrentString { get; private set; }

        public ParadoxParser(byte[] data, Action<ParadoxParser, string> parseStrategy, int bufferSize = Globals.BUFFER_SIZE)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (parseStrategy == null)
                throw new ArgumentNullException("parseStrategy");

            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", bufferSize, "Buffer size must be greater than 0");

            this.desiredBufferSize = bufferSize;
            this.buffer = new byte[desiredBufferSize];
            this.stringBuffer = new StringBuilder();

            using (stream = new MemoryStream(data))
            {
                parse(parseStrategy);
            }
        }

        public ParadoxParser(string filePath, Action<ParadoxParser, string> parseStrategy, int bufferSize = Globals.BUFFER_SIZE)
        {
            if (parseStrategy == null)
                throw new ArgumentNullException("parseStrategy");

            if (String.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", bufferSize, "Buffer size must be greater than 0");

            this.desiredBufferSize = bufferSize;
            this.buffer = new byte[desiredBufferSize];
            this.stringBuffer = new StringBuilder();

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

        private LexerToken GetNextToken()
        {
            while (IsSpace(currentByte = ReadByte()) && !eof)
                ;

            currentToken = IsSingleCharTok(currentByte);

            switch (currentToken)
            {
                case LexerToken.Comment:
                    while ((currentByte = ReadByte()) != NEWLINE && !eof)
                        ;
                    return GetNextToken();
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

        private string SaveBufferThenClear()
        {
            CurrentString = stringBuffer.ToString();
            stringBuffer.Clear();
            return CurrentString;
        }

        public byte ReadByte()
        {
            if (currentPosition == bufferSize)
            {
                if (!eof)
                    bufferSize = stream.Read(buffer, 0, desiredBufferSize);

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

            switch (GetNextToken())
            {
                case LexerToken.Quote:
                    while ((currentByte = ReadByte()) != QUOTE && !eof)
                        stringBuffer.Append((char)currentByte);

                    return SaveBufferThenClear();
                case LexerToken.Untyped:
                    do
                    {
                        stringBuffer.Append((char)currentByte);
                    } while (!IsSpace(currentByte = ReadByte()) && IsSingleCharTok(currentByte) == LexerToken.Untyped && !eof);

                    return SaveBufferThenClear();
                default:
                    return CurrentString = ReadString();
            }
            //return GetToken(stream);
        }

        public int ReadInt32()
        {
            int result;
            if (int.TryParse(ReadString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
                return result;
            throw new Exception();
        }

        public double ReadDouble()
        {
            double result;
            if (double.TryParse(ReadString(), SignedFloatingStyle, CultureInfo.InvariantCulture, out result))
                return result;
            throw new Exception();
        }

        public DateTime ReadDateTime()
        {
            DateTime result;
            if (DateTime.TryParseExact(ReadString(), "yyyy.M.d", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out result))
                return result;
            throw new Exception();
        }

        public void ReadInsideBrackets(Action<ParadoxParser> action)
        {
            int startingIndent = currentIndent;

            //Advance through the '{'
            GetNextToken();
            action(this);
            
            //Advance until the closing curly brace
            while (GetNextToken() != LexerToken.RightCurly && startingIndent == currentIndent && !eof)
                ;
        }
    }
}
