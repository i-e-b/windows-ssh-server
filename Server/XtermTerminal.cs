using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SshDotNet;

namespace WindowsSshServer
{
    public class XtermTerminal : Terminal
    {
        public XtermTerminal()
            : base()
        {
        }

        public override byte[] EscapeData(IEnumerable<KeyData> inputKeys)
        {
            using (var outputStream = new MemoryStream())
            {
                foreach (var key in inputKeys)
                {
                    outputStream.WriteByte(key.Value);
                }

                return outputStream.ToArray();
            }
        }

        public override KeyData[] UnescapeData(Stream inputStream)
        {
            var keys = new List<KeyData>();

            // Read escaped data from input stream.
            int inputByte;
            KeyData outputKey;

            while ((inputByte = inputStream.ReadByte()) != -1)
            {
                if (_bitMode == TerminalBitMode.Mode7Bit)
                {
                    // Check if current byte is start of control sequence.
                    if (inputByte == 0x1b) // ESC
                    {
                        outputKey = DecodeSequence7Bit(inputStream);
                    }
                    else
                    {
                        // Normal char
                        outputKey = new KeyData((byte)inputByte, false);
                    }
                }
                else if (_bitMode == TerminalBitMode.Mode8Bit)
                {
                    // Check if current byte is start of control sequence.
                    outputKey = DecodeSequence8Bit(inputStream);
                }
                else
                {
                    throw new InvalidOperationException("Bit mode is not valid for unescaping data.");
                }

                // Add current byte to output.
                keys.Add(outputKey);
            }

            return keys.ToArray();
        }

        protected KeyData DecodeSequence7Bit(Stream inputStream)
        {
            var keyData = new KeyData(0, true);

            // Read control character.
            var controlChar = (byte)inputStream.ReadByte();

            switch ((char)controlChar)
            {
                case '[': // CSI
                    keyData.Value = DecodeControlSequence(inputStream);
                    break;
                default:
                    XtermHelper.ThrowUnrecognisedCharException("Unrecognised 7-bit control character",
                        controlChar);
                    break;
            }

            return keyData;
        }

        protected KeyData DecodeSequence8Bit(Stream inputStream)
        {
            var keyData = new KeyData(0, true);

            // Read control character.
            var controlChar = (byte)inputStream.ReadByte();

            switch (controlChar)
            {
                case 0x9b: // CSI
                    keyData.Value = DecodeControlSequence(inputStream);
                    break;
                default:
                    keyData.Value = controlChar;
                    keyData.IsVirtualKey = false;
                    break;
            }

            return keyData;
        }

        protected byte DecodeControlSequence(Stream inputStream)
        {
            var controlChar = (byte)inputStream.ReadByte();

            switch ((char)controlChar)
            {
                case 'A':
                    return (byte)Keys.Up;
                case 'B':
                    return (byte)Keys.Down;
                case 'C':
                    return (byte)Keys.Right;
                case 'D':
                    return (byte)Keys.Left;
            }

            XtermHelper.ThrowUnrecognisedCharException("Unrecognised char in control sequence",
                controlChar);
            return 0;
        }
    }

    internal static class XtermHelper
    {
        public static void ThrowUnrecognisedCharException(string message, byte chr)
        {
            throw new InvalidOperationException(string.Format("{0} '{1}' (0x{2:X2}).", message,
                (char)chr, chr));
        }
    }

    //public struct EscapeSequence
    //{
    //    public char Char;       // Character that sequence represents.
    //    public string Sequence; // Sequence that represents character.

    //    public EscapeSequence(char chr, string sequence)
    //    {
    //        this.Char = chr;
    //        this.Sequence = sequence;
    //    }
    //}
}
