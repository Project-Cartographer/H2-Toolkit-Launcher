using System;
using System.Text;
using System.Collections.Generic;

namespace PETools
{
    class PEImageSymbol
    {
        string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        ushort offset;
        public ushort Offset
        {
            get { return offset; }
        }

        int index;
        UInt32 value;
        IMAGE_SECTION_NUMBER sectionNumber;
        IMAGE_SYMBOL_TYPE type;
        byte storageClass;
        byte numberOfAuxSymbols;

        public PEImageSymbol(IMAGE_SYMBOL symbol, int index)
        {
            this.index = index;
            this.value = symbol.Value;
            this.sectionNumber = symbol.SectionNumber;
            this.type = symbol.Type;
            this.storageClass = symbol.StorageClass;
            this.numberOfAuxSymbols = symbol.NumberOfAuxSymbols;

            // TODO: NULL and UNDEFINED symbols are ignored

            // if name[0..3] are all zero, name[4..7] contain offset into symbol table
            if (symbol.ShortName[0] == 0 &&
                symbol.ShortName[1] == 0 &&
                symbol.ShortName[2] == 0 &&
                symbol.ShortName[3] == 0)
            {
                this.offset = BitConverter.ToUInt16(symbol.ShortName, 4);
                this.name = null;
            }
            else
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                this.name = encoding.GetString(symbol.ShortName);
            }
        }

        public override string ToString()
        {
            return String.Format("\t[{0:X}] Name: {1,-25}\tType: {2,-20}\tSection: {3,-15}\n",
                index, name, type, sectionNumber);
        }
    }

    public class SymbolTable
    {
        PEImageSymbol[] symbols;

        public SymbolTable(uint numberOfSymbols)
        {
            symbols = new PEImageSymbol[numberOfSymbols];
        }

        public void AddSymbol(IMAGE_SYMBOL symbol, int index)
        {
            symbols[index] = new PEImageSymbol(symbol, index);
        }

        public bool AddString(string name, ushort offset)
        {
            bool found = false;
            foreach (PEImageSymbol s in symbols)
            {
                if ((s.Name == null) && (s.Offset == offset))
                {
                    s.Name = name;
                    found = true;
                }
            }
            return found;
        }

        public override string ToString()
        {
            String ret = "Symbol Table:\n";
            foreach (PEImageSymbol s in symbols)
                ret += s.ToString();
            return ret;
        }
    }
}
