using System;
using System.IO;
using System.Xml.Serialization;

namespace NLPTextTool
{
    public class Program
    {
        public class SDL2Entry
        {
            [XmlAttribute]
            public uint Unknown0;

            [XmlAttribute]
            public uint Unknown1;

            public SDL2.SDL2Data SDL2;
        }

        public class DBIN2
        {
            [XmlAttribute]
            public uint Key;

            [XmlAttribute]
            public uint Unknown;

            [XmlArrayItem("Entry")]
            public SDL2Entry[] Entries;
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("NLPTextTool - Experimental Text Dumper/Inserter for New Love Plus");
            Console.WriteLine("Made by gdkchan");
            Console.WriteLine("Version 0.1.0");
            Console.Write(Environment.NewLine);

            if (args.Length > 0)
            {
                foreach (string Argument in args)
                {
                    if (File.Exists(Argument))
                    {
                        switch (Path.GetExtension(Argument).ToLower())
                        {
                            case ".dbin2":
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write(string.Format("[{0}] ", Path.GetFileName(Argument)));

                                XmlSerializer Serializer = new XmlSerializer(typeof(DBIN2));
                                TextWriter Writer = new StreamWriter(Path.GetFileNameWithoutExtension(Argument) + ".xml");
                                Serializer.Serialize(Writer, Unbinarize(Argument));
                                Writer.Close();

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("Dumped successfully!");
                                break;

                            case ".xml":
                                Console.ForegroundColor = ConsoleColor.White;
                                string OutputFile = Path.GetFileNameWithoutExtension(Argument) + ".dbin2";
                                Console.Write(string.Format("[{0}] ", OutputFile));

                                XmlSerializer Deserializer = new XmlSerializer(typeof(DBIN2));
                                TextReader Reader = new StreamReader(Argument);
                                DBIN2 Data = (DBIN2)Deserializer.Deserialize(Reader);
                                Reader.Close();
                                Binarize(OutputFile, Data);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("Created successfully!");
                                break;
                        }

                        Console.Write(Environment.NewLine);
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Drag and drop files on this executable!");
            }

            Console.ResetColor();
        }

        /// <summary>
        ///     Transforms a binary DBIN2 file into a serializable object.
        /// </summary>
        /// <param name="FileName">The file name of the binary file</param>
        /// <returns>The object</returns>
        private static DBIN2 Unbinarize(string FileName)
        {
            DBIN2 Output = new DBIN2();

            using (FileStream DBIN2 = new FileStream(FileName, FileMode.Open))
            {
                EncryptedBinary Reader = new EncryptedBinary(DBIN2);

                string Signature = Reader.ReadString(4);
                if (Signature != "DBN2") throw new Exception("DBN2 signature not found!");
                Output.Key = Reader.Key = Reader.ReadUInt32();

                uint Entries = Reader.ReadUInt32XOrBE();
                Output.Unknown = Reader.ReadUInt32XOrBE();

                Output.Entries = new SDL2Entry[Entries];

                for (int i = 0; i < Entries; i++)
                {
                    DBIN2.Seek(0x10 + i * 0x10, SeekOrigin.Begin);

                    SDL2Entry Entry = new SDL2Entry();
                    Entry.Unknown0 = Reader.ReadUInt32XOrBE();
                    Entry.Unknown1 = Reader.ReadUInt32XOrBE();
                    uint Offset = Reader.ReadUInt32XOrBE();
                    uint Length = Reader.ReadUInt32XOrBE();

                    byte[] Buffer = new byte[Length];
                    DBIN2.Seek(Offset, SeekOrigin.Begin);
                    DBIN2.Read(Buffer, 0, Buffer.Length);
                    Entry.SDL2 = SDL2.GetDataFromBuffer(Buffer);

                    Output.Entries[i] = Entry;
                }
            }

            return Output;
        }

        /// <summary>
        ///     Transforms a object into a binary DBIN2 file.
        /// </summary>
        /// <param name="FileName">The output file name</param>
        /// <param name="Data">The object to be binarized</param>
        private static void Binarize(string FileName, DBIN2 Data)
        {
            using (FileStream Output = new FileStream(FileName, FileMode.Create))
            {
                EncryptedBinary Writer = new EncryptedBinary(Output);

                Writer.WriteString("DBN2");
                Writer.WriteUInt32(Data.Key);
                Writer.Key = Data.Key;

                Writer.WriteUInt32XOrBE((uint)Data.Entries.Length);
                Writer.WriteUInt32XOrBE(Data.Unknown);

                int DataOffset = 0x10 + Data.Entries.Length * 0x10;
                for (int i = 0; i < Data.Entries.Length; i++)
                {
                    Output.Seek(0x10 + i * 0x10, SeekOrigin.Begin);

                    byte[] Buffer = SDL2.GetBufferFromData(Data.Entries[i].SDL2);
                    Writer.WriteUInt32XOrBE(Data.Entries[i].Unknown0);
                    Writer.WriteUInt32XOrBE(Data.Entries[i].Unknown1);
                    Writer.WriteUInt32XOrBE((uint)DataOffset);
                    Writer.WriteUInt32XOrBE((uint)Buffer.Length);

                    Output.Seek(DataOffset, SeekOrigin.Begin);
                    Output.Write(Buffer, 0, Buffer.Length);
                    DataOffset += Buffer.Length;

                    //Align the buffer to 64-bits if needed
                    while ((DataOffset & 7) != 0)
                    {
                        Writer.WriteUInt8XOr(0);
                        DataOffset++;
                    }
                }
            }
        }
    }
}
