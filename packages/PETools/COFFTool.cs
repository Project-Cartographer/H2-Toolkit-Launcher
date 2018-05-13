using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace PETools
{
    public class COFFTool
    {
        public COFFTool(int ordinal)
        {
            this.ordinal = ordinal;
        }

        int ordinal;
        public int Ordinal
        {
            get { return ordinal; }
        }

        string sourceFile;
        public string SourceFile
        {
            get { return sourceFile; }
        }

        byte[] rawData;
        IMAGE_FILE_HEADER fileHeader;

        SymbolTable symbolTable;
        public SymbolTable SymbolTable
        {
            get { return symbolTable; }
        }

        List<PESection> sections;
        public List<PESection> Sections
        {
            get { return sections; }
        }

        public void Read(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                sourceFile = Path.GetFileName(filePath);
                Parse(stream);
                stream.Close();
            }
        }

        public void Read(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                Parse(stream);
                stream.Close();
            }
        }

        private void Parse(Stream stream)
        {
            rawData = new byte[stream.Length];
            stream.Read(rawData, 0, (int)stream.Length);
            stream.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(stream);

            fileHeader = PEUtility.FromBinaryReader<IMAGE_FILE_HEADER>(reader);

            // Read the sections
            sections = new List<PESection>();
            for (int i = 0; i < fileHeader.NumberOfSections; i++)
            {
                IMAGE_SECTION_HEADER header;
                header = PEUtility.FromBinaryReader<IMAGE_SECTION_HEADER>(reader);
                PESection section = new PESection(this, header);
                section.Parse(ref rawData);
                sections.Add(section);
            }

            // Read the symbol table from fileHeader.PointerToSymbolTable
            symbolTable = new SymbolTable(fileHeader.NumberOfSymbols);
            stream.Seek(fileHeader.PointerToSymbolTable, SeekOrigin.Begin);
            for (int i = 0; i < fileHeader.NumberOfSymbols; i++)
            {
                IMAGE_SYMBOL symbol;
                symbol = PEUtility.FromBinaryReader<IMAGE_SYMBOL>(reader);
                symbolTable.AddSymbol(symbol, i);
            }

            uint pointerToStringTable = fileHeader.PointerToSymbolTable +
                (uint)(fileHeader.NumberOfSymbols * Marshal.SizeOf(typeof(IMAGE_SYMBOL)));
            stream.Seek(pointerToStringTable, SeekOrigin.Begin);
            uint stringTableSize = PEUtility.FromBinaryReader<UInt32>(reader);

            for (ushort i = (ushort)Marshal.SizeOf(typeof(UInt32)); i < stringTableSize; )
            {
                String stringEntry = PEUtility.StringFromBinaryReader(reader);
                symbolTable.AddString(stringEntry, i);
                i += (ushort)(stringEntry.Length + 1); // include NULL terminator
            }

            Console.WriteLine("Object File: {0}", sourceFile);
            Console.WriteLine(symbolTable.ToString());
            Console.WriteLine("Sections:");
            foreach (PESection s in sections)
            {
                Console.WriteLine(s.ToString());
            }
            Console.WriteLine();
        }

        public static int Compare(COFFTool x, COFFTool y)
        {
            return x.Ordinal.CompareTo(y.Ordinal);
        }
    }
}
