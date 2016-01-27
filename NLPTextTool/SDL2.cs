using System;
using System.IO;
using System.Xml.Serialization;

namespace NLPTextTool
{
    public class SDL2
    {
        public struct SectionAEntry
        {
            public uint Value0;
            public uint Value1;
        }

        public struct UnknownEntry
        {
            [XmlArrayItem("Entry")]
            public SectionAEntry[] Values;

            public uint Value0;
            public uint Value1;
        }

        public class SDL2Data
        {
            [XmlAttribute]
            public uint Key;

            [XmlArrayItem("Entry")]
            public UnknownEntry[] Unknown;

            [XmlArrayItem("Dialog")]
            public string[] Dialogs;
        }

        /// <summary>
        ///     Gets the SDL2 data from a byte array buffer.
        /// </summary>
        /// <param name="Buffer">The buffer with the data</param>
        /// <returns>The SDL2 data</returns>
        public static SDL2Data GetDataFromBuffer(byte[] Buffer)
        {
            SDL2Data Output = new SDL2Data();

            using (MemoryStream SDL2 = new MemoryStream(Buffer))
            {
                EncryptedBinary Reader = new EncryptedBinary(SDL2);
                
                string Signature = Reader.ReadString(4);
                if (Signature != "SDL2") throw new Exception("SDL2 signature not found!");
                Output.Key = Reader.Key = Reader.ReadUInt32();

                ushort Table0Count = Reader.ReadUInt16XOrBE();
                ushort DialogsCount = Reader.ReadUInt16XOrBE();

                Output.Unknown = new UnknownEntry[Table0Count];

                for (int i = 0; i < Table0Count; i++)
                {
                    uint SectionBOffset = Reader.ReadUInt32XOrBE();
                    uint SectionAOffset = Reader.ReadUInt32XOrBE();
                    long Position = SDL2.Position;

                    UnknownEntry Entry = new UnknownEntry();

                    SDL2.Seek(SectionAOffset, SeekOrigin.Begin);
                    uint Entries = Reader.ReadUInt32XOrBE();
                    Entry.Values = new SectionAEntry[Entries];
                    for (int j = 0; j < Entries; j++)
                    {
                        Entry.Values[j].Value0 = Reader.ReadUInt32XOrBE();
                        Entry.Values[j].Value1 = Reader.ReadUInt32XOrBE();
                    }

                    SDL2.Seek(SectionBOffset, SeekOrigin.Begin);
                    Entry.Value0 = Reader.ReadUInt32XOrBE();
                    Entry.Value1 = Reader.ReadUInt32XOrBE();
                    Output.Unknown[i] = Entry;

                    SDL2.Seek(Position, SeekOrigin.Begin);
                }

                Output.Dialogs = new string[DialogsCount];

                for (int i = 0; i < DialogsCount; i++)
                {
                    uint DialogOffset = Reader.ReadUInt32XOrBE();
                    long Position = SDL2.Position;

                    SDL2.Seek(DialogOffset, SeekOrigin.Begin);
                    Output.Dialogs[i] = Reader.ReadUTF8();

                    SDL2.Seek(Position, SeekOrigin.Begin);
                }
            }

            return Output;
        }

        /// <summary>
        ///     Creates a buffer from the SDL2 data.
        /// </summary>
        /// <param name="Data">The data to be binarized</param>
        /// <returns>The data as a byte array</returns>
        public static byte[] GetBufferFromData(SDL2Data Data)
        {
            using (MemoryStream Output = new MemoryStream())
            {
                EncryptedBinary Writer = new EncryptedBinary(Output);

                Writer.WriteString("SDL2");
                Writer.WriteUInt32(Data.Key);
                Writer.Key = Data.Key;

                Writer.WriteUInt16XOrBE((ushort)Data.Unknown.Length);
                Writer.WriteUInt16XOrBE((ushort)Data.Dialogs.Length);

                int SectionALength = Data.Unknown.Length * 4;
                foreach (UnknownEntry Entry in Data.Unknown) SectionALength += Entry.Values.Length * 8;
                int SectionAOffset = 0xc + Data.Unknown.Length * 8 + Data.Dialogs.Length * 4;
                int SectionBOffset = SectionAOffset + SectionALength;
                int DialogsOffset = SectionBOffset + Data.Unknown.Length * 8;

                for (int i = 0; i < Data.Unknown.Length; i++)
                {
                    Writer.WriteUInt32XOrBE((uint)SectionBOffset);
                    Writer.WriteUInt32XOrBE((uint)SectionAOffset);
                    long Position = Output.Position;

                    Output.Seek(SectionAOffset, SeekOrigin.Begin);
                    Writer.WriteUInt32XOrBE((uint)Data.Unknown[i].Values.Length);
                    SectionAOffset += 4;
                    foreach (SectionAEntry Entry in Data.Unknown[i].Values)
                    {
                        Writer.WriteUInt32XOrBE(Entry.Value0);
                        Writer.WriteUInt32XOrBE(Entry.Value1);
                        SectionAOffset += 8;
                    }

                    Output.Seek(SectionBOffset, SeekOrigin.Begin);
                    Writer.WriteUInt32XOrBE(Data.Unknown[i].Value0);
                    Writer.WriteUInt32XOrBE(Data.Unknown[i].Value1);
                    SectionBOffset += 8;

                    Output.Seek(Position, SeekOrigin.Begin);
                }

                for (int i = 0; i < Data.Dialogs.Length; i++)
                {
                    Writer.WriteUInt32XOrBE((uint)DialogsOffset);
                    long Position = Output.Position;

                    Output.Seek(DialogsOffset, SeekOrigin.Begin);
                    DialogsOffset += Writer.WriteUTF8(Data.Dialogs[i]);
                    while ((DialogsOffset & 3) != 0)
                    {
                        //Align into 32-bits if needed
                        Writer.WriteUInt8XOr(0);
                        DialogsOffset++; 
                    }

                    Output.Seek(Position, SeekOrigin.Begin);
                }

                return Output.ToArray();
            }
        }
    }
}
