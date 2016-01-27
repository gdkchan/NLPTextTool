using System.IO;
using System.Text;

namespace NLPTextTool
{
    public class EncryptedBinary
    {
        public Stream BaseStream;

        private byte[] KeyBytes = new byte[3];
        private byte Seed;

        /// <summary>
        ///     The Key used to Read or Write "XOr" encryted data.
        /// </summary>
        public uint Key
        {
            set
            {
                KeyBytes[0] = (byte)value;
                KeyBytes[1] = (byte)(value >> 8);
                KeyBytes[2] = (byte)(value >> 16);
                Seed = (byte)(value >> 24);
            }
        }

        /// <summary>
        ///     Creates a new encrypted binary from a Stream.
        /// </summary>
        /// <param name="BaseStream"></param>
        public EncryptedBinary(Stream BaseStream)
        {
            this.BaseStream = BaseStream;
        }

        /// <summary>
        ///     Closes the underlying Stream.
        /// </summary>
        public void Close()
        {
            BaseStream.Close();
        }

        /*
         * Reads
         */

        //Decrypted

        /// <summary>
        ///     Reads a unencrypted 8-bits value from the Stream.
        /// </summary>
        /// <returns>The unencrypted value</returns>
        public byte ReadUInt8()
        {
            return (byte)BaseStream.ReadByte();
        }

        /// <summary>
        ///     Reads a unencrypted 16-bits value from the Stream.
        /// </summary>
        /// <returns>The unencrypted value</returns>
        public ushort ReadUInt16()
        {
            return (ushort)(ReadUInt8() | (ReadUInt8() << 8));
        }

        /// <summary>
        ///     Reads a unencrypted 32-bits value from the Stream.
        /// </summary>
        /// <returns>The unencrypted value</returns>
        public uint ReadUInt32()
        {
            return (uint)(ReadUInt8() | (ReadUInt8() << 8) | (ReadUInt8() << 16) | (ReadUInt8() << 24));
        }

        /// <summary>
        ///     Reads a ASCII string from the Stream with the given number of bytes.
        /// </summary>
        /// <param name="Length">The total number of bytes</param>
        /// <returns>The string</returns>
        public string ReadString(int Length)
        {
            StringBuilder Output = new StringBuilder();
            while (Length-- > 0) Output.Append((char)ReadUInt8());
            return Output.ToString();
        }

        //Encrypted

        /// <summary>
        ///     Reads and decrypts a 8-bits value from the Stream.
        /// </summary>
        /// <returns>The decrypted value</returns>
        public byte ReadUInt8XOr()
        {
            long Position = BaseStream.Position;
            int Value = BaseStream.ReadByte();
            byte Decrypted = (byte)(Value ^ (KeyBytes[Position % 3] + (Seed * (Position / 3))));
            return Decrypted;
        }

        /// <summary>
        ///     Reads and decrypts a 16-bits value from the Stream.
        /// </summary>
        /// <returns>The decrypted value</returns>
        public ushort ReadUInt16XOr()
        {
            return (ushort)(ReadUInt8XOr() | (ReadUInt8XOr() << 8));
        }

        /// <summary>
        ///     Reads and decrypts a 16-bits value from the Stream in Big Endian format.
        /// </summary>
        /// <returns>The decrypted value</returns>
        public ushort ReadUInt16XOrBE()
        {
            return (ushort)((ReadUInt8XOr() << 8) | ReadUInt8XOr());
        }

        /// <summary>
        ///     Reads and decrypts a 32-bits value from the Stream.
        /// </summary>
        /// <returns>The decrypted value</returns>
        public uint ReadUInt32XOr()
        {
            return (uint)(ReadUInt8XOr() | (ReadUInt8XOr() << 8) | (ReadUInt8XOr() << 16) | (ReadUInt8XOr() << 24));
        }

        /// <summary>
        ///     Reads and decrypts a 32-bits value from the Stream in Big Endian format.
        /// </summary>
        /// <returns>The decrypted value</returns>
        public uint ReadUInt32XOrBE()
        {
            return (uint)((ReadUInt8XOr() << 24) | (ReadUInt8XOr() << 16) | (ReadUInt8XOr() << 8) | ReadUInt8XOr());
        }

        /// <summary>
        ///     Reads and decrypts a UTF-8 text with a given number of bytes from the Stream.
        /// </summary>
        /// <returns>The text</returns>
        public string ReadUTF8()
        {
            int Length = ReadUInt16XOrBE();
            byte[] Buffer = new byte[Length];
            for (int i = 0; i < Length; i++) Buffer[i] = ReadUInt8XOr();
            return Encoding.UTF8.GetString(Buffer);
        }
        
        /*
         * Writes
         */

        //Decrypted

        /// <summary>
        ///     Writes a unencrypted 8 bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt8(byte Value)
        {
            BaseStream.WriteByte(Value);
        }

        /// <summary>
        ///     Writes a unencrypted 16 bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt16(ushort Value)
        {
            WriteUInt8((byte)Value);
            WriteUInt8((byte)(Value >> 8));
        }

        /// <summary>
        ///     Writes a unencrypted 32 bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt32(uint Value)
        {
            WriteUInt8((byte)Value);
            WriteUInt8((byte)(Value >> 8));
            WriteUInt8((byte)(Value >> 16));
            WriteUInt8((byte)(Value >> 24));
        }

        /// <summary>
        ///     Writes a ASCII string to the Stream.
        /// </summary>
        /// <param name="String">The string to be written</param>
        public void WriteString(string String)
        {
            byte[] Data = Encoding.ASCII.GetBytes(String);
            foreach (byte Byte in Data) WriteUInt8(Byte);
        }

        //Encrypted

        /// <summary>
        ///     Encrypts and writes a 8-bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt8XOr(byte Value)
        {
            long Position = BaseStream.Position;
            byte Encrypted = (byte)(Value ^ (KeyBytes[Position % 3] + (Seed * (Position / 3))));
            BaseStream.WriteByte(Encrypted);
        }

        /// <summary>
        ///     Encrypts and writes a 16-bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt16XOr(ushort Value)
        {
            WriteUInt8XOr((byte)Value);
            WriteUInt8XOr((byte)(Value >> 8));
        }

        /// <summary>
        ///     Encrypts and writes a 16-bits value to the Stream in Big Endian format.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt16XOrBE(ushort Value)
        {
            WriteUInt8XOr((byte)(Value >> 8));
            WriteUInt8XOr((byte)Value);
        }

        /// <summary>
        ///     Encrypts and writes a 32-bits value to the Stream.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt32XOr(uint Value)
        {
            WriteUInt8XOr((byte)Value);
            WriteUInt8XOr((byte)(Value >> 8));
            WriteUInt8XOr((byte)(Value >> 16));
            WriteUInt8XOr((byte)(Value >> 24));
        }

        /// <summary>
        ///     Encrypts and writes a 32-bits value to the Stream in Big Endian format.
        /// </summary>
        /// <param name="Value">The value to be written</param>
        public void WriteUInt32XOrBE(uint Value)
        {
            WriteUInt8XOr((byte)(Value >> 24));
            WriteUInt8XOr((byte)(Value >> 16));
            WriteUInt8XOr((byte)(Value >> 8));
            WriteUInt8XOr((byte)Value);
        }

        /// <summary>
        ///     Encrypts and writes a UTF-8 text to the Stream.
        /// </summary>
        /// <param name="Text">The text to be written</param>
        public int WriteUTF8(string Text)
        {
            byte[] Data = Encoding.UTF8.GetBytes(Text);
            WriteUInt16XOrBE((ushort)Data.Length);
            foreach (byte Byte in Data) WriteUInt8XOr(Byte);
            return Data.Length + 2;
        }
    }
}
