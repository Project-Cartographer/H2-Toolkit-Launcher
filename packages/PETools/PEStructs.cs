using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PETools
{
    public struct IMAGE_DOS_HEADER
    {
        public UInt16 e_magic;              // Magic number
        public UInt16 e_cblp;               // Bytes on last page of file
        public UInt16 e_cp;                 // Pages in file
        public UInt16 e_crlc;               // Relocations
        public UInt16 e_cparhdr;            // Size of header in paragraphs
        public UInt16 e_minalloc;           // Minimum extra paragraphs needed
        public UInt16 e_maxalloc;           // Maximum extra paragraphs needed
        public UInt16 e_ss;                 // Initial (relative) SS value
        public UInt16 e_sp;                 // Initial SP value
        public UInt16 e_csum;               // Checksum
        public UInt16 e_ip;                 // Initial IP value
        public UInt16 e_cs;                 // Initial (relative) CS value
        public UInt16 e_lfarlc;             // File address of relocation table
        public UInt16 e_ovno;               // Overlay number
        public UInt16 e_res_0;              // Reserved words
        public UInt16 e_res_1;              // Reserved words
        public UInt16 e_res_2;              // Reserved words
        public UInt16 e_res_3;              // Reserved words
        public UInt16 e_oemid;              // OEM identifier (for e_oeminfo)
        public UInt16 e_oeminfo;            // OEM information; e_oemid specific
        public UInt16 e_res2_0;             // Reserved words
        public UInt16 e_res2_1;             // Reserved words
        public UInt16 e_res2_2;             // Reserved words
        public UInt16 e_res2_3;             // Reserved words
        public UInt16 e_res2_4;             // Reserved words
        public UInt16 e_res2_5;             // Reserved words
        public UInt16 e_res2_6;             // Reserved words
        public UInt16 e_res2_7;             // Reserved words
        public UInt16 e_res2_8;             // Reserved words
        public UInt16 e_res2_9;             // Reserved words
        public UInt32 e_lfanew;             // File address of new exe header
    }

    public struct IMAGE_NT_HEADERS
    {
        public UInt32 Signature;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt32 BaseOfData;
        public UInt32 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public UInt16 Subsystem;
        public UInt16 DllCharacteristics;
        public UInt32 SizeOfStackReserve;
        public UInt32 SizeOfStackCommit;
        public UInt32 SizeOfHeapReserve;
        public UInt32 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt64 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public UInt16 Subsystem;
        public UInt16 DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;
    }

    [Flags]
    public enum PECharacteristics : UInt16
    {
        IMAGE_FILE_RELOCS_STRIPPED = 0x0001,
        IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002,
        IMAGE_FILE_LINE_NUMS_STRIPPED = 0x0004,
        IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x0008,
        IMAGE_FILE_AGGRESIVE_WS_TRIM = 0x0010,
        IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020,
        IMAGE_FILE_BYTES_REVERSED_LO = 0x0080,
        IMAGE_FILE_32BIT_MACHINE = 0x0100,
        IMAGE_FILE_DEBUG_STRIPPED = 0x0200,
        IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 0x0400,
        IMAGE_FILE_NET_RUN_FROM_SWAP = 0x0800,
        IMAGE_FILE_SYSTEM = 0x1000,
        IMAGE_FILE_DLL = 0x2000,
        IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000,
        IMAGE_FILE_BYTES_REVERSED_HI = 0x8000,
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_FILE_HEADER
    {
        public IMAGE_FILE_MACHINE Machine;
        public UInt16 NumberOfSections;
        public UInt32 TimeDateStamp;
        public UInt32 PointerToSymbolTable;
        public UInt32 NumberOfSymbols;
        public UInt16 SizeOfOptionalHeader;
        public PECharacteristics Characteristics;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_DATA_DIRECTORIES
    {
        public IMAGE_DATA_DIRECTORY_ENTRY export;
        public IMAGE_DATA_DIRECTORY_ENTRY import;
        public IMAGE_DATA_DIRECTORY_ENTRY resource;
        public IMAGE_DATA_DIRECTORY_ENTRY exception;
        public IMAGE_DATA_DIRECTORY_ENTRY security;
        public IMAGE_DATA_DIRECTORY_ENTRY baseReloc;
        public IMAGE_DATA_DIRECTORY_ENTRY debug;
        public IMAGE_DATA_DIRECTORY_ENTRY copyright;
        public IMAGE_DATA_DIRECTORY_ENTRY architecture;
        public IMAGE_DATA_DIRECTORY_ENTRY globalPtr;
        public IMAGE_DATA_DIRECTORY_ENTRY tls;
        public IMAGE_DATA_DIRECTORY_ENTRY loadConfig;
        public IMAGE_DATA_DIRECTORY_ENTRY boundImport;
        public IMAGE_DATA_DIRECTORY_ENTRY iat;
        public IMAGE_DATA_DIRECTORY_ENTRY delayImport;
        public IMAGE_DATA_DIRECTORY_ENTRY comDescriptor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_DATA_DIRECTORY_ENTRY
    {
        public UInt32 VirtualAddress;
        public UInt32 Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_SECTION_HEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] SectionName;
        public UInt32 PhysicalAddressOrVirtualSizeUnion;
        public UInt32 VirtualAddress;
        public UInt32 SizeOfRawData;
        public UInt32 PointerToRawData;
        public UInt32 PointerToRelocations;
        public UInt32 PointerToLinenumbers;
        public UInt16 NumberOfRelocations;
        public UInt16 NumberOfLinenumbers;
        public UInt32 Characteristics;
    }

    public enum IMAGE_SECTION_FLAGS : uint
    {
        IMAGE_SCN_CNT_CODE = 0x00000020,  // Section contains code.
        IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040,  // Section contains initialized data.
        IMAGE_SCN_CNT_UNINITIALIZED_DATA = 0x00000080,  // Section contains uninitialized data.
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_RELOCATION
    {
        public UInt32 VirtualAddress;
        public UInt32 SymbolTableIndex;
        public IMAGE_REL Type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_SYMBOL
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ShortName;
        public UInt32 Value;
        public IMAGE_SECTION_NUMBER SectionNumber;
        public IMAGE_SYMBOL_TYPE Type;
        public byte StorageClass;
        public byte NumberOfAuxSymbols;
    }

    public enum IMAGE_SECTION_NUMBER : short
    {
        IMAGE_SYM_UNDEFINED          = 0,
        IMAGE_SYM_ABSOLUTE           = -1,
        IMAGE_SYM_DEBUG              = -2,
    }

    public enum IMAGE_SYMBOL_TYPE : ushort
    {
        IMAGE_SYM_TYPE_NULL                = 0x0000,
        IMAGE_SYM_TYPE_VOID                = 0x0001,
        IMAGE_SYM_TYPE_CHAR                = 0x0002,
        IMAGE_SYM_TYPE_SHORT               = 0x0003,
        IMAGE_SYM_TYPE_INT                 = 0x0004,
        IMAGE_SYM_TYPE_LONG                = 0x0005,
        IMAGE_SYM_TYPE_FLOAT               = 0x0006,
        IMAGE_SYM_TYPE_DOUBLE              = 0x0007,
        IMAGE_SYM_TYPE_STRUCT              = 0x0008,
        IMAGE_SYM_TYPE_UNION               = 0x0009,
        IMAGE_SYM_TYPE_ENUM                = 0x000A,
        IMAGE_SYM_TYPE_MOE                 = 0x000B,
        IMAGE_SYM_TYPE_BYTE                = 0x000C,
        IMAGE_SYM_TYPE_WORD                = 0x000D,
        IMAGE_SYM_TYPE_UINT                = 0x000E,
        IMAGE_SYM_TYPE_DWORD               = 0x000F,
        // Special Microsoft flag
        IMAGE_SYM_TYPE_MSFT_FN             = 0x0020,
        IMAGE_SYM_TYPE_PCODE               = 0x8000,
    }

    public enum IMAGE_FILE_MACHINE : ushort
    {
        IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
        IMAGE_FILE_MACHINE_I386 = 0x14c,
        IMAGE_FILE_MACHINE_AMD64 = 0x8664,
    }

    public enum IMAGE_REL : ushort
    {
        // i386 relocs
        IMAGE_REL_I386_ABSOLUTE        = 0x0000,
        IMAGE_REL_I386_DIR16           = 0x0001,
        IMAGE_REL_I386_REL16           = 0x0002,
        IMAGE_REL_I386_DIR32           = 0x0006,
        IMAGE_REL_I386_DIR32NB         = 0x0007,
        IMAGE_REL_I386_SEG12           = 0x0009,
        IMAGE_REL_I386_SECTION         = 0x000A,
        IMAGE_REL_I386_SECREL          = 0x000B,
        IMAGE_REL_I386_TOKEN           = 0x000C,
        IMAGE_REL_I386_SECREL7         = 0x000D,
        IMAGE_REL_I386_REL32           = 0x0014,

        // x64 relocs
        //IMAGE_REL_AMD64_ABSOLUTE       = 0x0000,
        //IMAGE_REL_AMD64_ADDR64         = 0x0001,
        //IMAGE_REL_AMD64_ADDR32         = 0x0002,
        //IMAGE_REL_AMD64_ADDR32NB       = 0x0003,
        //IMAGE_REL_AMD64_REL32          = 0x0004,
        //IMAGE_REL_AMD64_REL32_1        = 0x0005,
        //IMAGE_REL_AMD64_REL32_2        = 0x0006,
        //IMAGE_REL_AMD64_REL32_3        = 0x0007,
        //IMAGE_REL_AMD64_REL32_4        = 0x0008,
        //IMAGE_REL_AMD64_REL32_5        = 0x0009,
        //IMAGE_REL_AMD64_SECTION        = 0x000A,
        //IMAGE_REL_AMD64_SECREL         = 0x000B,
        //IMAGE_REL_AMD64_SECREL7        = 0x000C,
        //IMAGE_REL_AMD64_TOKEN          = 0x000D,
        //IMAGE_REL_AMD64_SREL32         = 0x000E,
        //IMAGE_REL_AMD64_PAIR           = 0x000F,
        //IMAGE_REL_AMD64_SSPAN32        = 0x0010,
    }

    /*
     * A struct to contain the data of each section.
     * More of a convenience structure, also store section name
     * and index for easy lookups.
     */
    public struct SectionData
    {
        public byte[] data;
        public string name;
        public int index;
    }
}
