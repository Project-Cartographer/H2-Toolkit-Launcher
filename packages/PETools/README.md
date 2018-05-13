PETools
=======

A C# Library for handling and manipulating this Portable Executable format.

Useful for modifying section data of a DLL.

Some preliminary work for supporting COFF with basic ability to read symbol tables.

Usage
-----

```csharp

using PETools;

PETool pe = new PETool();

pe.Read("target.dll");

using (MemoryStream stream = new MemoryStream(pe.GetSectionData(".data")))
{
    // read contents using `stream` object
    stream.Close()
}


// It's also possible to replace section data, e.g.:
byte[] data = ...;
pe.WriteSectionData(".data", data);

// Call layout to fix up headers and section layout
pe.Layout();

// Write it back to disk
pe.WriteFile("target.dll");

```
