using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class Terminal
    {
        protected TerminalBitMode _bitMode;         // Bit mode of terminal.

        protected List<EscapeSequence> _escapeSeqs; // List of escape sequences.

        public Terminal()
        {
            _bitMode = TerminalBitMode.Mode7Bit;
        }

        public ReadOnlyCollection<EscapeSequence> EscapeSequences
        {
            get
            {
                return new ReadOnlyCollection<EscapeSequence>(_escapeSeqs);
            }
        }

        public byte[] EscapeData(KeyData[] input)
        {
            //

            return null;
        }

        public KeyData[] UnescapeData(byte[] input)
        {
            using (var inputStream = new MemoryStream(input))
            {
                return UnescapeData(inputStream);
            }
        }

        public KeyData[] UnescapeData(Stream inputStream)
        {
            var keys = new List<KeyData>();

            // Read escaped data from input stream.
            int inputByte;
            KeyData outputKey;

            while ((inputByte = inputStream.ReadByte()) != -1)
            {
                if (_bitMode == TerminalBitMode.Mode7Bit)
                {
                    if (inputByte == 27) // ESC
                        outputKey = new KeyData(DecodeSequence7Bit(inputStream), true);
                    else // Normal char
                        outputKey = new KeyData((byte)inputByte, false);
                }
                else if (_bitMode == TerminalBitMode.Mode8Bit)
                {
                    //outputKey = new KeyData(DecodeSequence8Bit(inputStream), true);

                    // Normal char
                    outputKey = new KeyData((byte)inputByte, false);
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

        protected byte DecodeSequence7Bit(Stream inputStream)
        {
            // Read control character.
            var controlChar = (char)inputStream.ReadByte();

            switch (controlChar)
            {
                case '[': // CSI
                    return DecodeControlSequence(inputStream);
            }

            throw new InvalidOperationException(string.Format("Unrecognised 7-bit control character" +
                " '{0}' (0x{1:X2}).", controlChar, (byte)controlChar));
        }

        protected byte DecodeSequence8Bit(Stream inputStream)
        {
            // Read control character.
            var controlChar = (char)inputStream.ReadByte();

            switch (controlChar)
            {
                //
            }

            throw new InvalidOperationException(string.Format("Unrecognised char in control sequence" +
                " '{0}' (0x{1:X2}).", controlChar, (byte)controlChar));
        }

        protected byte DecodeControlSequence(Stream inputStream)
        {
            var controlChar = (char)inputStream.ReadByte();

            switch (controlChar)
            {
                case 'A':
                    return 37; // Cursor Up
                case 'B':
                    return 38; // Cursor Down
                case 'C':
                    return 39; // Cursor Right
                case 'D':
                    return 40; // Cursor Left
            }

            return (byte)inputStream.ReadByte();
        }
    }

    public struct KeyData
    {
        public byte Value;
        public bool IsVirtualKey;

        public KeyData(byte value, bool isVirtualKey)
        {
            this.Value = value;
            this.IsVirtualKey = isVirtualKey;
        }
    }

    public struct EscapeSequence
    {
        public char Char;       // Character that sequence represents.
        public string Sequence; // Sequence that represents character.

        public EscapeSequence(char chr, string sequence)
        {
            this.Char = chr;
            this.Sequence = sequence;
        }
    }

    public enum TerminalBitMode
    {
        Mode7Bit,
        Mode8Bit
    }
}
