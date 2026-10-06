# Nwn.Authoring

Authoring model for Neverwinter Nights tools, built on Nwn.Formats. Targets `net10.0`.

- Resource resolution across ordered layers (loose directories, ERF/HAK/MOD, KEY/BIF) with provenance.
- Undoable document sessions over GFF and JSON documents; atomic file transactions with recovery.
- Areas: creation, property editing, tile painting, procedural generation, instance placement.
- Palette categories and standard palette import.
- Model resolution for creatures, items, placeables and doors; door, waypoint, sound and trigger data.

```csharp
using Nwn.Authoring.Resources;
using Nwn.Formats.Gff;
using Nwn.Formats.Resources;

using var layer = ResourceLayer.FromLooseDirectory("module", @"C:\modules\mymod");
using var resolver = new ResourceResolver([layer]);

var found = resolver.Resolve(new ResourceIdentity("area001", ResourceType.Are));
if (found is not null)
{
    GffDocument are = GffReader.Read(found.Bytes);
}
```

Part of the Nwn.* toolset libraries (Nwn.Formats, Nwn.Authoring, Nwn.Preview, Nwn.Toolset.Avalonia). Released as previews; APIs may change before 1.0.

Licensed under MIT. Some source was extracted from SWLOR (MIT, copyright 2019 Zunath); its notice is in `licenses/SWLOR-MIT.txt` inside the package.
