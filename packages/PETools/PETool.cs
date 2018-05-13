using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PETools
{
    public partial class PETool
    {
        public IMAGE_DOS_HEADER dosHeader;
        public byte[] dosStub;
        public IMAGE_NT_HEADERS ntSignature;
        public IMAGE_FILE_HEADER fileHeader;
        public IMAGE_OPTIONAL_HEADER32 optionalHeader;
        public IMAGE_DATA_DIRECTORIES dataDirectories;

        List<PESection> sections;

        /// <summary>
        /// Raw data content of entire PE.
        /// </summary>
        public byte[] rawData;

        bool Is32BitHeader
        {
            get
            {
                return (PECharacteristics.IMAGE_FILE_32BIT_MACHINE & fileHeader.Characteristics) == PECharacteristics.IMAGE_FILE_32BIT_MACHINE;
            }
        }

        /// <summary>
        /// Read a PE file.
        /// </summary>
        /// <param name="filePath">Path to PE file.</param>
        public void Read(string filePath)
        {
            // Read in the DLL or EXE and get the timestamp
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                Parse(stream);
                stream.Close();
            }
        }

        /// <summary>
        /// Read a PE file.
        /// </summary>
        /// <param name="data">Contents of a PE as a byte array.</param>
        public void Read(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                Parse(stream);
                stream.Close();
            }
        }

        /// <summary>
        /// Parse a PE.
        /// </summary>
        /// <param name="stream">A stream of the PE contents.</param>
        private void Parse(Stream stream)
        {
            rawData = new byte[stream.Length];
            stream.Read(rawData, 0, (int)stream.Length);
            stream.Seek(0, SeekOrigin.Begin);

            BinaryReader reader = new BinaryReader(stream);
            dosHeader = PEUtility.FromBinaryReader<IMAGE_DOS_HEADER>(reader);

            int stubSize = (int)dosHeader.e_lfanew - Marshal.SizeOf(typeof(IMAGE_DOS_HEADER));
            dosStub = reader.ReadBytes(stubSize);

            // Add 4 bytes to the offset
            stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
            ntSignature = PEUtility.FromBinaryReader<IMAGE_NT_HEADERS>(reader);
            fileHeader = PEUtility.FromBinaryReader<IMAGE_FILE_HEADER>(reader);
            optionalHeader = PEUtility.FromBinaryReader<IMAGE_OPTIONAL_HEADER32>(reader);
            dataDirectories = PEUtility.FromBinaryReader<IMAGE_DATA_DIRECTORIES>(reader);

            sections = new List<PESection>();
            for (int i = 0; i < fileHeader.NumberOfSections; i++)
            {
                IMAGE_SECTION_HEADER header = PEUtility.FromBinaryReader<IMAGE_SECTION_HEADER>(reader);
                PESection section = new PESection(header);
                section.Parse(ref rawData);
                sections.Add(section);
            }
        }

        /// <summary>
        /// Layout contents of PE file, updating headers and order sections.
        /// </summary>
        /// <returns>Returns bool describing if layout succeeded.</returns>
        public bool Layout()
        {
            uint virtualAlignment = optionalHeader.SectionAlignment;
            uint fileAlignment = optionalHeader.FileAlignment;
            uint totalSize = 0;
            uint initializedDataSize = 0;

            totalSize += optionalHeader.SizeOfHeaders;
            /* Calculate total physical size required */
            foreach (PESection s in sections)
            {
                totalSize += PEUtility.AlignUp((uint)s.Data.Length, fileAlignment);
            }

            /* Layout the sections in physical order */
            uint filePosition = optionalHeader.SizeOfHeaders;
            sections.Sort(new SectionPhysicalComparer());
            foreach (PESection s in sections)
            {
                if (s.ContributesToFileSize())
                {
                    s.RawSize = PEUtility.AlignUp((uint)s.Data.Length, fileAlignment);
                    s.PhysicalAddress = filePosition;

                    filePosition += s.RawSize;
                    initializedDataSize += PEUtility.AlignUp((uint)s.Data.Length, fileAlignment);
                }
                break;
            }

            optionalHeader.SizeOfInitializedData = initializedDataSize;

            /*
             * Fix up virtual addresses of the sections.
             * We start at 0x1000 (seems to be the convention)
             * Text should come first, then followed by data, then reloc
             * As we encounter certain sections, we need to update
             * special fields (data directory entries etc.).
             */
            uint virtAddr = 0x1000;
            bool dataSectionEncountered = false;
            sections.Sort(new SectionVirtualComparer());
            foreach (PESection s in sections)
            {
                if (s.Name == ".text")
                {
                    optionalHeader.BaseOfCode = virtAddr;
                }

                if (!dataSectionEncountered &&
                    ((s.Name == ".data") || (s.Name == ".rdata")))
                {
                    dataSectionEncountered = true;
                    optionalHeader.BaseOfData = virtAddr;
                }

                if (s.Name == ".rdata")
                {
                    dataDirectories.debug.VirtualAddress = virtAddr;
                }

                if (s.Name == ".reloc")
                {
                    dataDirectories.baseReloc.VirtualAddress = virtAddr;
                }

                s.VirtualAddress = virtAddr;

                if (s.HasUninitializedData)
                {
                    // Leave uninitialized data sizes untouched, their raw size is 0
                    virtAddr += PEUtility.AlignUp(s.VirtualSize, virtualAlignment);
                }
                else if (s.HasInitializedData && s.HasCode)
                {
                    // It is possible for the virtual size to be greater than the size of raw data
                    // Leave the virtual size untouched if this is the case
                    if (s.VirtualSize > s.RawSize)
                    {
                        virtAddr += PEUtility.AlignUp(s.VirtualSize, virtualAlignment);
                    }
                    else
                    {
                        s.VirtualSize = (uint)s.Data.Length;
                        virtAddr += PEUtility.AlignUp((uint)s.Data.Length, virtualAlignment);
                    }
                }

                break;
            }

            /* Total virtual size is the final virtual address, which includes the initial virtual offset. */
            optionalHeader.SizeOfImage = virtAddr;

            /* Serialize and write the header contents */
            Serialize(totalSize);

            return true;
        }

        private void SortSectionsForLayout()
        {

        }

        public void UpdateHeader()
        {
            SerializeHeader(ref rawData);
        }

        private uint SerializeHeader(ref byte[] file)
        {
            uint filePosition = 0;

            Array.Copy(PEUtility.RawSerialize(dosHeader), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_DOS_HEADER)));
            filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_DOS_HEADER));

            Array.Copy(dosStub, 0, file, filePosition, dosStub.Length);
            filePosition += (uint)dosStub.Length;

            Array.Copy(PEUtility.RawSerialize(ntSignature), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_NT_HEADERS)));
            filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_NT_HEADERS));

            Array.Copy(PEUtility.RawSerialize(fileHeader), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_FILE_HEADER)));
            filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_FILE_HEADER));

            Array.Copy(PEUtility.RawSerialize(optionalHeader), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_OPTIONAL_HEADER32)));
            filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_OPTIONAL_HEADER32));

            return filePosition;
        }

            private void Serialize(uint totalSize)
        {
            /* Allocate enough space to contain the whole new file */
            byte[] file = new byte[totalSize];
            uint filePosition = SerializeHeader(ref file);

            Array.Copy(PEUtility.RawSerialize(dataDirectories), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_DATA_DIRECTORIES)));
            filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_DATA_DIRECTORIES));

            // XXX: Sections must be sorted in layout order!
            foreach (PESection section in sections)
            {
                Array.Copy(PEUtility.RawSerialize(section.Header), 0, file, filePosition, Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)));
                filePosition += (uint)Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER));
            }

            /* Copy the section data */
            filePosition = optionalHeader.SizeOfHeaders;
            sections.Sort(new SectionPhysicalComparer());
            foreach (PESection s in sections)
            {
                    Array.Copy(s.Data, 0, file, filePosition, s.Data.Length);
                    filePosition += s.RawSize;
                    break;
            }

            /* Overwrite the container data */
            rawData = file;
        }

        /// <summary>
        /// Write contents of PE.
        /// </summary>
        /// <param name="filename">Path of file to write to.</param>
        public void WriteFile(string filename)
        {
            /* Flush the contents of rawData back to disk */
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            fs.Write(rawData, 0, rawData.Length);
            fs.Close();
        }

        public void AddCOFFSections(List<PESection> sections)
        {

        }

        /// <summary>
        /// Write the contents of the provided byte array into the section specified.
        /// </summary>
        /// <param name="name">Name of section</param>
        /// <param name="data">Byte array of section data</param>
        /// <returns></returns>
        public uint WriteSectionData(string name, byte[] data)
        {
            PESection section = sections.Find(s => s.Name == name);
            if (section == null)
                return 0;

            section.Data = new byte[data.Length];
            Array.Copy(data, 0, section.Data, 0, data.Length);
            return (uint)data.Length;
        }

        /// <summary>
        /// Retrieve the contents of the specified section.
        /// </summary>
        /// <param name="name">Name of section whose contents should be retrieved</param>
        /// <returns>Byte array of section contents</returns>
        public byte[] GetSectionData(string name)
        {
            PESection section = sections.Find(s => s.Name == name);
            if (section == null)
                return null;
            else
                return section.Data;
        }

    }
}
