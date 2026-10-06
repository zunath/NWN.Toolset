# Compiled skeleton hierarchy

The compiled MDL header's node count bounds the local hierarchy; it is not
necessarily its length. The [NWN compiler](https://github.com/niv/nwn-tools/blob/master/_NmcLib/NmcGeometry.cpp)
initializes that count after the supermodel's count and increments it when
assigning each local node's part number. The [geometry header](https://github.com/niv/nwn-tools/blob/master/_NwnLib/NwnMdlGeometry.h)
places this field at model-data offset `0x4C`.

The reader rejects a local hierarchy larger than the header count. It retains
the configured count, byte, depth, pointer-array and geometry bounds and rejects
cycles/shared pointers. Traversal does not allocate from an unbounded header.

Compiled nodes have pointer identities independent of their names. The native
female skeleton contains two leaf nodes named `Impact` under `torso_g`. Keeping
both names and distinct identities preserves their native transforms. ASCII
nodes retain their unique names as identities; ambiguous ASCII references still
fail. Scene preparation resolves parent identities, and assembly scopes both
identities and material lookups by instance.

Read-only Xenomech `xm_pt_root` fixtures:

| Model | Header count | Local nodes | SHA-256 |
| --- | ---: | ---: | --- |
| pmh0 | 329 | 56 | 1ecd5849cb8f08d920dc17c18cfd2db63f6e8bb37b184ff41ed4884364b1959f |
| pfh0 | 446 | 57 | 82ffd41c6233ce400f11819cae242b672eec2590a091d72ab1720d8d37ff9c9b |
| pmx0 | 221 | 56 | 2d2aa38c4dde726cfa2769848f0296db51e4510f73e2597aa5954ea94514cb79 |
| pfx0 | 221 | 56 | 9cbbb3efea3051e2166d5ca1e18c5a9e246562d54924a2cc18eec0244dc672e2 |

Qualification: 142 format tests and 41 preview tests pass on Windows and a fresh,
offline Linux .NET SDK 10.0.401 build, with zero skips. Evidence remains in
`tests/Nwn.Formats.Tests/TestResults/native-node-identities-formats-windows-final.trx`,
`tests/Nwn.Preview.Tests/TestResults/native-node-identities-preview-windows.trx`
and `artifacts/linux-scene/native-node-identities-Nwn.*-linux.trx`.
The native female test verifies both impact transforms and unique parent/child
identities after assembling two skeleton instances. Fixtures carry no local
animation tracks. Inherited animation evaluation and official-client movement
are separate gates.

Compiled orientation controllers store quaternion components in `X, Y, Z, W`
order. The identity tuple `(0, 0, 0, 1)` therefore leaves the native rest
skeleton unchanged. Reading it as `W, X, Y, Z` rotates every joint and displaces
attached parts. Synthetic identity and quarter-turn fixtures and the four
SHA-pinned native skeletons cover the order independently of assembly math.

After this correction, 143 format tests and 41 preview tests pass on Windows
and fresh offline Linux builds with zero skips. The Windows receipts are
`compiled-quaternion-formats-windows.trx` and
`compiled-quaternion-preview-windows.trx` in their projects' `TestResults`;
Linux receipts are `artifacts/linux-scene/compiled-quaternion-Nwn.*-linux.trx`.
Xenomech also loads all six native fits and renders an Eshari female body,
head and authored armor through the real OpenGL viewport. Inspection of that
framebuffer establishes correct static attachment for the rendered fixture;
it does not establish animated or equipped-client behavior.
