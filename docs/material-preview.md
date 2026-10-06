# Material reading and palette composition

`Nwn.Formats.Mtr.MtrReader` reads material descriptors without loading shader
programs. It recognizes texture slots 0–14, vertex/fragment/geometry shader
bindings, render hints and finite float/int parameters. All `customshader` binding
names also remain available through `RawShaderBindings`, including legacy VSH/PSH
names; lookup ignores case while enumeration preserves the source spelling.
Parameter names retain case. `TypeName` and `RawValues` retain the original type
token and value tokens, including unsupported parameter types. Supported values
also expose their numeric type and components; resource spellings remain unchanged. These basic bindings
follow [Beamdog's material announcement](https://store.steampowered.com/news/posts/?appids=704450&enddate=1520446160&feed=steam_community_announcements).

Input bytes, line length and directive count are bounded. Supported duplicate
bindings and malformed numeric parameters fail with their source line. Unknown
directives and empty or multi-token texture bindings remain explicit
`UnrecognizedDirectives`. Unsupported parameter types have `Uninterpreted` kind
and no numeric components. Callers decide whether they can interpret raw bindings
or parameters. `CopySourceBytes` returns the original bytes including comments,
line endings, encoding preamble and uninterpreted lines. The document and its
collections cannot be mutated through the public API.

`Nwn.Preview.Tint.PackedTintMaskDecoder` reads a red-channel shade and a
green-channel layer bin into immutable PLT-style texels. The caller chooses the
number of equally spaced bins. `PltCompositor` resolves texels through explicit
caller-selected palette rows. A `PltLayerTint` can instead choose an exact RGBA
color shaded relative to the supplied neutral row's middle shade. Both forms
can coexist. A black middle shade uses a bounded denominator; highlights clamp
to the output channel range. Every encountered layer needs an explicit choice.

The library does not select a game's shader, material parameter names, number of
armor layers, neutral metal/skin rows, item-local variables, fitting convention
or tint precedence. Callers own those decisions. The composed surface is an
unlit color preview: shader execution, lighting, animation, skinned assembly,
normal/specular maps and native equipped appearance remain separate gates.

The viewport chooses a supplied explicit material surface before the bitmap
surface, falling back when the material surface is unavailable. Material and
bitmap bindings can therefore identify different decoded surfaces.

Compiled rigid MDL meshes retain the fourth 64-byte binding as `MaterialName`,
independently of their bitmap. A production body part with only that binding
qualifies the read; it does not establish assembly, skinning or equipped fit.

The DDS reader supports unsigned ATI2/BC5 with independent red and green
channels, blue zero and opaque alpha. Its endpoint interpolation follows
[Microsoft's BC5 layout](https://learn.microsoft.com/en-us/windows/win32/direct3d10/d3d10-graphics-programming-guide-resources-block-compression),
quantized to RGBA8. Signed BC5 and DX10 headers remain explicit unsupported inputs.
Complete declared mip payloads are checked before allocation; a valid one-mip
file is accepted. Partial edge blocks are cropped to the declared dimensions.

`DdsDecodeOptions.StoredRowOrder` makes caller policy explicit. Format default
normalizes compact BioWare's bottom-up rows and keeps standard DDS's usual
top-down order. Hosts reading NWN-authored standard ATI2 explicitly choose
`BottomUp`. Row reversal works per pixel row, including partial block heights.
Returned images are top-down. Palette shader coordinates remain a separate
caller decision.

## Qualification

The reader preserves every selected source material: 3,757 Xenomech files and
8,843 SWLOR files. It retains 79 and 98 uninterpreted lines respectively. The
corpus tests fail for missing fixtures, supported parse failures or changed
source bytes. Reading these files does not establish engine behavior for their
uninterpreted directives or shader programs.

Windows source regressions pass Formats 136/136, Preview 37/37, Authoring 25/25
and Avalonia 3/3 with zero skipped, with a zero-warning solution build. The same
complete regressions pass in the fixed Linux SDK image
`mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29`,
with networking disabled and source, NuGet cache and both fixture corpora mounted
read-only. The writable output and copied build tree are isolated.

Windows evidence is in Xenomech's `artifacts/tests/toolset-authoring`:
`shared-native-codec-{formats,authoring,toolset.avalonia}.trx` and
`shared-bc5-native-preview.trx`.
Linux evidence is under
`artifacts/linux-native-codec-final-20261001/logs`, with its exact command script
beside the logs. Package consumer adoption and official-client color comparisons
must be recorded separately before claiming a complete appearance workflow.

The real Windows OpenGLES 3.0 control reads back a 768×768 framebuffer. All four
asymmetric texture quadrants have the expected orientation; the explicit material
surface wins over a magenta bitmap fallback. A repeated frame reuses one mesh
and one texture upload. This establishes material selection, texture orientation
and upload reuse, without qualifying native equipped appearance.

Native ATI2 hashes match the independent Python decoder for a 2048×2048
Xenomech mask with twelve mips and a 512×512 SWLOR mask with one mip. Compact
BioWare hashes match Pillow decoding followed by bottom-up row normalization.
Xenomech retains the reproducible oracle script in
`artifacts/tests/toolset-authoring/derive-dds-oracle.py`; tests bind the ATI2
proofs to the source asset hashes. These are byte and orientation checks, not
native GPU or lighting comparisons.
