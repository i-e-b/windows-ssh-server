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

        public byte[] EscapeData(byte[] input)
        {
            using (var inputStream = new MemoryStream(input))
            {
                return EscapeData(inputStream);
            }
        }

        public byte[] EscapeData(Stream inputStream)
        {
            using (var outputStream = new MemoryStream())
            {
                //

                return outputStream.ToArray();
            }
        }

        public byte[] UnescapeData(byte[] input)
        {
            using (var inputStream = new MemoryStream(input))
            {
                return UnescapeData(inputStream);
            }
        }

        public byte[] UnescapeData(Stream inputStream)
        {
            using (var outputStream = new MemoryStream())
            {
                // Read escaped data from input stream.
                int inputByte;
                byte outputByte;

                while ((inputByte = inputStream.ReadByte()) != -1)
                {
                    outputByte = 0;

                    if (_bitMode == TerminalBitMode.Mode7Bit)
                    {
                        if (inputByte == 27) // ESC
                            outputByte = DecodeSequence7Bit(inputStream);
                        else // Normal char
                            outputByte = (byte)inputByte;
                    }
                    else if (_bitMode == TerminalBitMode.Mode8Bit)
                    {
                        //outputByte = DecodeSequence8Bit(inputStream);

                        // Normal char
                        outputByte = (byte)inputByte;
                    }
                    else
                    {
                        throw new InvalidOperationException("Bit mode is not valid for unescaping data.");
                    }

                    // Output current byte.
                    outputStream.WriteByte(outputByte);
                }

                return outputStream.ToArray();
            }
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
