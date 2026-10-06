# Nwn.Preview

CPU-side preview support for Neverwinter Nights tools, built on Nwn.Formats and Nwn.Authoring. Targets `net10.0`. No GPU or UI dependency.

- TGA, DDS and PLT decoding to RGBA images; texture resource loading.
- Native model animation sampling, part composition and prepared (render-ready) scenes.
- Area scene composition, picking, walkmesh and instance manipulation math.
- Thumbnails, type icons and bounded preview caches.

```csharp
using Nwn.Preview.Pixels;
using Nwn.Preview.Tga;

RgbaImage image = TgaDecoder.Decode(File.ReadAllBytes("icon.tga"));
Console.WriteLine($"{image.Width}x{image.Height}");
```

Part of the Nwn.* toolset libraries (Nwn.Formats, Nwn.Authoring, Nwn.Preview, Nwn.Toolset.Avalonia). Released as previews; APIs may change before 1.0.

Licensed under MIT. Some source was extracted from SWLOR (MIT, copyright 2019 Zunath); its notice is in `licenses/SWLOR-MIT.txt` inside the package.
