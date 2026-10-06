# Nwn.Formats

Managed readers and writers for Neverwinter Nights resource formats. Targets `net10.0`. No engine, UI or third-party dependencies.

Covers GFF (binary and JSON), ERF/HAK/MOD, KEY/BIF, 2DA, TLK, MDL (ASCII and binary), MTR and tileset SET files. Readers use bounded allocation budgets so malformed input fails with an exception instead of exhausting memory.

```csharp
using Nwn.Formats.Gff;
using Nwn.Formats.TwoDa;

GffDocument are = GffReader.Read(File.ReadAllBytes("area001.are"));
TwoDaTable placeables = TwoDaReader.Read(File.ReadAllText("placeables.2da"));
```

Part of the Nwn.* toolset libraries (Nwn.Formats, Nwn.Authoring, Nwn.Preview, Nwn.Toolset.Avalonia). Released as previews; APIs may change before 1.0.

Licensed under MIT. Some source was extracted from SWLOR (MIT, copyright 2019 Zunath); its notice is in `licenses/SWLOR-MIT.txt` inside the package.
