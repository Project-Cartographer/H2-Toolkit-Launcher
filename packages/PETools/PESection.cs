using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PETools
{
    public class PESection
    {
        COFFTool sourceCoff;
        public COFFTool SourceCoff
        {
            get { return sourceCoff; }
        }

        IMAGE_SECTION_HEADER header;
        public IMAGE_SECTION_HEADER Header
        {
            get { return header; }
        }

        byte[] sectionData;
        public byte[] Data
        {
            get { return sectionData; }
            set { sectionData = value; }
        }

        List<IMAGE_RELOCATION> relocations;

        public PESection(IMAGE_SECTION_HEADER header)
        {
            this.header = header;
        }

        public PESection(COFFTool coff, IMAGE_SECTION_HEADER header)
        {
            this.sourceCoff = coff;
            this.header = header;
            this.relocations = null;
        }

        public String Name
        {
            get
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] bytes = new byte[8];
                int len = 0;
                foreach (char c in header.SectionName)
                {
                    if (c == '\0')
                        break;
                    bytes[len] = (byte)c;
                    len++;
                }
                return encoding.GetString(bytes, 0, len);
            }
            set
            {
                char[] chars = value.ToCharArray();
                Array.Clear(header.SectionName, 0, 8);
                Array.Copy(chars, header.SectionName, chars.Length);

            }
        }

        public bool HasRelocations
        {
            get { return (header.NumberOfRelocations != 0); }
        }

        public bool HasUninitializedData
        {
            get { return ((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_UNINITIALIZED_DATA) != 0); }
        }

        public bool HasInitializedData
        {
            get { return ((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_INITIALIZED_DATA) != 0); }
        }

        public bool HasCode
        {
            get { return ((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_CODE) != 0); }
        }

        public uint VirtualAddress
        {
            get { return header.VirtualAddress; }
            set { header.VirtualAddress = value; }
        }

        public uint VirtualSize
        {
            get { return header.PhysicalAddressOrVirtualSizeUnion; }
            set { header.PhysicalAddressOrVirtualSizeUnion = value; }
        }

        public uint PhysicalAddress
        {
            get { return header.PointerToRawData; }
            set { header.PointerToRawData = value; }
        }

        public uint RawSize
        {
            get { return header.SizeOfRawData; }
            set { header.SizeOfRawData = value; }
        }

        public bool ContributesToFileSize()
        {
            bool contributes_to_filesize = false;

            if ((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_INITIALIZED_DATA) != 0)
            {
                contributes_to_filesize = true;
            }
            else if (((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_CODE) != 0) &&
                        ((header.Characteristics & (uint)IMAGE_SECTION_FLAGS.IMAGE_SCN_CNT_UNINITIALIZED_DATA) == 0))
            {
                contributes_to_filesize = true;
            }

            return contributes_to_filesize;
        }

        public void Parse(ref byte[] file)
        {
            // Differentiate between COFF object and PE image.
            uint sectionSize = header.SizeOfRawData;
            if (header.PhysicalAddressOrVirtualSizeUnion > 0)
             sectionSize = Math.Min(
                 header.SizeOfRawData,
                 header.PhysicalAddressOrVirtualSizeUnion);

            sectionData = new byte[sectionSize];
            // Make a copy of the section data
            Array.Copy(file, header.PointerToRawData,
                sectionData, 0,
                sectionSize);

            ParseRelocations(ref file);
        }

        void ParseRelocations(ref byte[] file)
        {
            relocations = new List<IMAGE_RELOCATION>();

            if (!HasRelocations)
                return;

            MemoryStream stream = new MemoryStream(file);
            stream.Seek(header.PointerToRelocations, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(stream);

            for (int i = 0; i < header.NumberOfRelocations; i++)
            {
                IMAGE_RELOCATION reloc;
                reloc = PEUtility.FromBinaryReader<IMAGE_RELOCATION>(reader);
                relocations.Add(reloc);
            }
        }

        public static int Compare(PESection x, PESection y)
        {
            if (!x.Name.Contains("$") && !y.Name.Contains("$"))
            {
                // COFF file ordinal is used as tie-breaker
                return COFFTool.Compare(x.SourceCoff, y.SourceCoff);
            }

            // Always give preference to sections with no $
            else if (!x.Name.Contains("$") && y.Name.Contains("$"))
                return -1;
            else if (x.Name.Contains("$") && !y.Name.Contains("$"))
                return 1;

            // If both have a $ grouping, order by $ suffix.
            else // (x.Name.Contains("$") && y.Name.Contains("$"))
            {
                string xdollar = x.Name.Substring(x.Name.IndexOf('$') + 1);
                string ydollar = y.Name.Substring(x.Name.IndexOf('$') + 1);

                int cmp = String.Compare(xdollar, ydollar);
                // COFF file ordinal is used as tie-breaker
                if (cmp == 0)
                    return COFFTool.Compare(x.SourceCoff, y.SourceCoff);
                else
                    return cmp;
            }
        }

        public override string ToString()
        {
            String ret = String.Empty;
            ret += String.Format("Name: {0,-15}\tVirt Addr: {1:X}\tVirt Size: {2:X}\tPhys Addr: {3:X}\tRaw Size: {4:X}",
                Name, VirtualAddress, VirtualSize, PhysicalAddress, RawSize);

            if ((relocations == null) || (relocations.Count == 0))
                return ret;

            ret += "\n\tRelocation fixups:\n";
            foreach (IMAGE_RELOCATION reloc in relocations)
            {
                ret += String.Format("\tIndex: {0:X}\tVirt Addr: {1:X} Type: {2}\n",
                    reloc.SymbolTableIndex, reloc.VirtualAddress, reloc.Type);
            }

            return ret;
        }
    }
}
