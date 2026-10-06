# Shared area builder source

Commit ids cited below refer to the libraries' development history before this repository was published; that history was not preserved, so those ids do not resolve here.

The owner authorizes moving SWLOR's existing area builder into the shared libraries
and using it in both applications. The area builder is extracted, with application
policies supplied by each host. There is no separate Xenomech tile or placement
implementation.

The first extracted source is from the isolated `codex/toolset-swlor` worktree at
`391634c4e`. Its parser, definitions, adjacency, solver, paint operations and tile
palette move from `SWLOR.Toolset.Domain/GameData/Tilesets`. SET parsing and data
types belong to `Nwn.Formats/Tilesets`; area editing operations belong to
`Nwn.Authoring/Areas/Tiles`. Public types each have their own file. SWLOR's callers
use their shared namespaces, and the old implementation files are removed.

Extraction changes namespaces and file ownership. Tile matching, corner heights,
quarter-turn orientation, group holes, palette label preference and brush behavior
retain their source implementation. In particular, group names precede TLK lookup
because custom tilesets may contain stale copied string references. The caller
supplies TLK resolution; the libraries do not select a game's custom TLK.

First-party SWLOR source uses the MIT license, copyright 2019 Zunath, retained in
`licenses/SWLOR-MIT.txt` and included in packages containing extracted source.
The historical GPL notice is described in SWLOR's `SWLOR.Toolset/LICENSE-NOTICE.md`;
it does not license this first-party area source or introduce a current dependency.

The same source revision supplies the lexical nwn_gff document model, guarded
edit operations and undo stack, native ARE/GIT/IFO views and area template factory.
These move into `Nwn.Authoring/Documents` and `Nwn.Authoring/Editing`. Native field
types, numeric tokens, localization entries and unrelated fields retain their
source representation. The native bridge preserves SWLOR's existing sorted JSON
behavior by default; a host can request native field order when adapting binary
sources. Sessions accept a host codec while retaining SWLOR's lexical codec as
their default. SWLOR's old binary model needs only a representation adapter.

The neutral resource-reference shape check is shared as
`Nwn.Formats/Resources/ResourceReferenceRules`; SWLOR removes its duplicate helper.
Xenomech's area facade delegates name, tag and tile writes to the extracted
`AreDocument`/`AreaTiles`. Its source adapter preserves its distinct native/JSON
representation and JSON annotations. It contains no second tile solver or painter.

## Qualified extraction

Local immutable packages are `Nwn.Formats 0.1.0-dev.20` (SHA-256
`cca308f243e3404ce706f113b8661ec97c25e7c0eb1880fe2639f53350df52b0`)
and `Nwn.Authoring 0.1.0-dev.18` (SHA-256
`32d6cf7b13fa11670d756c4152dfe49fbbc1fceedaecd364a4d76b4d998e2e35`).
Both isolated applications build against them. These are local packages, not
remote publications.

- Existing SWLOR source regressions: 131 area/tile/corpus checks and 610
  document/editor/corpus checks, all passing with zero skips.
- SWLOR package consumer: 199 focused area/session/placement checks, zero skips,
  `area-extraction-package-session-tiles.trx`.
- Shared formats: 143/143 and authoring: 29/29 on Windows and fresh offline Linux,
  zero skips. Authoring includes native bridge round trips and native host codec
  edit/snapshot/reload checks.
- Xenomech package integration: 75/75 authoring checks on Windows and fresh offline
  Linux, including native/JSON tile edits, undo and unknown metadata preservation;
  architecture 72/72 and clean Release solution build.

SWLOR tests explicitly select the owner's read-only module and HAK corpus. Counts
no longer assume exactly 936 creatures when the corpus contains 940. The lexical
integer-edit fixture selects an actual integer-bearing document; a separate
journal no-edit check retains coverage of the journal without mutable integers.
Initial missing-corpus, stale-count and bridge field-order failures remain in the
evidence directories and do not count as passes. Linux uses pinned SDK
`10.0.401@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29`,
read-only source/package/corpus mounts and no network.

Placement mapping/GIC, scene and viewport extraction, and the complete graphical
integration remain required. These checks do not qualify native interactions or
official-client behavior, and the full completion goal remains active.

## Native placement move

The subsequent move takes `InstanceFieldMap` and `GicDocument` from SWLOR commit
`f0554b923` into `Nwn.Authoring/Areas/Placement` and `Documents/Native`.
`ModuleResourceType` moves the existing module-kind enum into shared authoring;
SWLOR retains its file-naming, palette-order and display-name extensions. Global
type aliases connect existing SWLOR callers without retaining a duplicate enum.
Position, facing, enhanced-edition transforms, trigger polygons, deep copies,
instance struct ids and parallel comments keep their implementation.

These types ship in local immutable `Nwn.Authoring 0.1.0-dev.19`, SHA-256
`0a0ed44f4cf80bd1f7035f6b756791f55943590ad69c81b36d9d46aadd6212f2`.
The actual SWLOR package consumer passes 206/206 placement/scene/workspace/palette
regressions with zero skips in `area-placement-extraction-package-corpus.trx`.
The initial source run's skipped palette override is retained; its corrected
explicit corpus run passes 12/12. A fresh source build reports eight pre-existing
nullable warnings in SWLOR's runtime/API projects and no errors; the moved shared
types build without warnings. Scene, viewport, atomic area creation and the
complete Xenomech graphical integration still require extraction and adoption.

## Mutable native MDL reader and mesh producer

The SWLOR mutable NWN MDL records and their binary/ASCII readers move to
`Nwn.Formats/NativeModels`; the independent immutable `Nwn.Formats/Mdl` API
remains available. Their existing guarded random-access reader, allocation
budget and NWN text-encoding implementations move to their corresponding
`Io`, `Reading` and `Text` format concerns. `NwnFormatException` is shared in
`Nwn.Formats/Exceptions`, retaining its two constructors, `FormatException`
base and parser messages; SWLOR's other readers and consumers alias that same
type, so catches still observe failures from the moved readers.

The existing animation sampler and `MdlMeshBuilder` move to
`Nwn.Preview/Scene`. Native format data remains separate from render data;
host-specific resource lookup, model-chain resolution, appearance selection,
armor classification and skin-clearance values are supplied by the caller.
Typed build options carry existing render-purpose selectors and a source-mesh
callback for host metadata. The parser and mesh algorithms retain their SWLOR
behavior; the shared API does not reference game/runtime assemblies or SWLOR
identifiers. SWLOR's application, tests and animation tools now consume the
shared parser and producer, and the old mutable parser and producer files are
removed. The first-party source remains covered by `licenses/SWLOR-MIT.txt`.

Immutable packages are `Nwn.Formats 0.1.0-dev.22`, SHA-256
`13f560f01a219d49b2e8ea7c3fd0fb5abf3f528026203591ec52f874a1dd9c5f`, and
`Nwn.Preview 0.1.0-dev.24`, SHA-256
`6f9461216a0770b3a12672d1f0640b71cd4a403e2787535ce8adf70a8a67388e`.
Formats22 identifies source commit
`e721d5fa24e315511b9c858d8cbbcd3cbe485b6a`; Preview24 identifies
`72258dd0cc91dadb0cf632bed2cde15bb8cacd55` and depends directly on
Authoring 0.1.0-dev.21 and Formats 0.1.0-dev.22. Old package archives were
left unchanged.

Windows source-mode regressions passed with explicit read-only corpora:
Formats 144/144, Preview 45/45, SWLOR MDL reader 33/33, animation/skinmesh/
filter/placeable 40/40, render/composition 53/53, and the HAK MDL parse sweep
1/1. Package mode passed Formats 95/95, the SWLOR parser/render filter 84/84,
the HAK MDL sweep 1/1, and the area/assembly/resource/appearance integration
filter 94/94. All reported passes had zero skips. The last filter's TRX is
`SWLOR.Toolset.Tests/TestResults/mdl-area-adapter-package-final.trx`.

The shared source passed offline Linux tests in the pinned
SDK image `10.0.401@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29`,
with network disabled and source, package cache and both native corpora mounted
read-only: Formats 144/144 and Preview 47/47, zero skips. The Preview run
includes parity tests for hidden placeable geometry and empty animation clip
lists. TRX files are
`artifacts/swlor-mdl-extraction/linux-parity-review/linux-final-Formats.trx`
and `.../linux-final-Preview.trx`.

An initial package area filter had four corpus-selection failures; the two
legacy door-anchor/cache tests were updated to use the explicit repository and
HAK selectors, and the rerun passed 94/94. The failed run remains separate
evidence and is not counted as a pass. Preview's shared mesh producer and the
SWLOR composer/cache remain separate because the composer carries game
classification and resource policy.

The next viewport boundary is the existing SWLOR resource index/catalog,
texture loader/cache and `GlAreaControl`, not a replacement renderer. SWLOR
must keep its game install and layer configuration, tint catalog, material
binding interpretation and `ArmorPart` metadata. A neutral viewport can accept
host-resolved surfaces/palette uniforms and mesh classifications through a
typed provider; shared resource lookup should accept explicit ordered layers.
The renderer's algorithms and shaders remain the source of behavior. Native
appearance and official-client gates remain outside this extraction.

Preview24 fixes options-overload parity: `PlaceablePreview` retains the hidden
selection surface when it is the only placeable geometry, and `AnimatedPreview`
reduces multi-pose input to the settled pose even with no named clips. Side-by-
side tests compare each options overload to its existing convenience method.
The Preview suite passed 47/47 on Windows and offline Linux with zero skips.
After adopting Preview24, SWLOR's focused hidden-placeable, animation,
skinmesh and composition tests passed 50/50 with zero skips in
`SWLOR.Toolset.Tests/TestResults/mdl-options-parity-package.trx`.

## Resource resolution and texture loading

`Nwn.Authoring/Resources` exposes `ResourceResolver.ResolveHandle` for metadata-only
existence and provenance checks, and `ResourceResolutionHandle.ReadBytes(maximumBytes)`
for a caller-bounded read. The existing eager `Resolve` API delegates to the handle.
Loose files, ERF entries and selected KEY/BIF resources enforce the effective
per-layer and per-call byte limit before allocating their payload. ERF-backed
handles reopen their explicitly configured archive for each read, so an in-flight
handle remains usable across an index generation replacement. The neutral resource
type table retains the full SWLOR extension set, including `gff`; ERF/BIF/KEY
container extensions remain addressable but are not accepted as resource type codes.

The SWLOR `ResourceIndex` now uses that resolver for HAK layers, keeps its own
`hakbuilder.json` and module HAK-stack discovery, and swaps a complete resolver
generation atomically. Its existence-only `Contains` calls do not read payloads.
The base-game `KeyBifCatalog` remains an SWLOR adapter for existing consumers and
is the next resource-reader migration boundary.

`Nwn.Preview/Textures/TextureResourceLoader` provides the neutral TGA/DDS/PLT
preference, bounded decode and palette-row composition API. A host supplies the
resource reader, palette source and standard-DDS row orientation. `PltCompositor`
accepts palette images keyed by layer and typed row selections. `BoundedLruCache`
provides count- and byte-limited retention; SWLOR keeps its material/tint key,
shader interpretation and palette naming in its adapter.

The source-mode shared suites passed with explicit native roots: Formats 146/146,
Authoring 37/37 and Preview 51/51, all with zero skips. SWLOR source-mode focused
tests passed ResourceIndex/orientation/material adapter 28/28, native DDS/MTR/PLT
and tint/cache cases 4/4, and channel-order corpus 3/3, zero skips. The channel
fixture now uses `NWN_INSTALL_PATH`, `SWLOR_TEST_REPOSITORY_ROOT` and
`SWLOR_TEST_HAKS_ROOT`, so isolated worktrees can use the explicitly selected
read-only installation and complete HAK corpus. Earlier runs lacking those
selectors are not counted as qualification.

The immutable consumer train is Formats `0.1.0-dev.23` (SHA-256
`49F04AB87117CDA54D2F2FD55BA9FA8F97CBADC7CD2B6894E41FC691909A1C00`),
Authoring `0.1.0-dev.22` (SHA-256
`DD3C063169A382152D8ACE247D077475BB95674FAE18D022A696B39FDC67417B`) and
Preview `0.1.0-dev.25` (SHA-256
`62B10516074F589F1B035608CD6932CACCEC1014F50CC687CB472169A71BA85E`). All
three nuspecs identify source commit
`f6aacb570d970b85259094a97fffcc62eb23ef92`; Authoring depends on Formats23 and
Preview depends on Authoring22/Formats23. Older package archives remain
unchanged. The fixed Linux SDK qualification reran the full source suites
offline with zero skips: Formats 146/146, Authoring 37/37 and Preview 51/51.

## Blueprint-to-placement synchronization

`Nwn.Authoring/Areas/Placement` contains the native blueprint synchronization
algorithms used by the SWLOR editor. `BlueprintInstanceSynchronizer` rebuilds
placed instances from a blueprint while preserving placement-owned position,
orientation, trigger geometry, and visual transforms; rename operations update
resource references without rebuilding placement overrides. Store synchronization
retains the existing inventory reconstruction behavior and accepts a host-supplied
item-blueprint resolver. The supported placed resource kinds are UTC, UTD, UTI,
UTP, UTS, UTM, UTT, and UTW; other kinds fail explicitly.

The source was moved from `SWLOR.Toolset.Domain/Documents` at SWLOR commit
`4d1db02cfb852923b88aad0d504b03647ef71615` under the scoped area-builder
extraction authorization. The moved source retains its MIT notice in
`licenses/SWLOR-MIT.txt`. SWLOR remains responsible for item blueprint lookup and
its application save policy; the shared library owns only GFF document transforms.

The immutable package train built from source commit
`ceefc61c3e8ad81b857180336ab356f969b802eb` is Authoring `0.1.0-dev.27` (SHA-256
`FD613B405B4B336EBDE1AB191533DC9BE7B0A67762A590684A66878DA951A5BC`), Preview
`0.1.0-dev.32` (SHA-256
`4301C5143D6240C32325A99F98E6F1CB17670BAA78A9F5B614308F2DC8FD4B97`), and Avalonia
`0.1.0-dev.20` (SHA-256
`0449A940782CB12CBD9CF168E52BA185E65C4227AA9771E89984523960E0A96E`). The nuspecs
identify that exact commit; Authoring depends on Formats25, Preview on Authoring27/Formats25,
and Avalonia on Authoring27/Preview32. Windows and pinned offline Linux Authoring suites each
pass 64/64 with zero skips, including the resource resolver inventory regression.

## Placed-object detail form

`Nwn.Toolset.Avalonia.Areas.AreaInstanceDetailForm` moves the existing tag,
XYZ position, XY facing and trigger width/height rows out of
`SWLOR.Toolset/Editors/Views/AreaEditorView.axaml` at source commit
`d5b41a93d6ce9f9b2ec62f33ac11d317f68fea3a`. The field ordering, grid dimensions,
increments and trigger minima are retained. Captions are supplied by each host
through `AreaInstanceDetailLabels`; SWLOR retains its existing captions.
`IAreaInstanceDetailFormState` matches the existing SWLOR view-model properties.
SWLOR's transactions, behavior editors and local-variable table stay with its
application. Xenomech supplies selected-document lifetime and localization while
using the same extracted `InstanceFieldMap` transforms and history.

This is a presentation extraction from the already implemented SWLOR area
builder, not a second area builder. The MIT attribution remains in
`licenses/SWLOR-MIT.txt`. Further behavior-specific property and transition
integration, official-client qualification and performance acceptance remain
separate requirements.

Both real hosts pin Avalonia `0.1.0-dev.21`, built from source commit
`912bef308f611a5653c1f79a9cdbcfaa906c8dba`, with SHA-256
`2f5e456f9fd8e42379cf60763a99a7eaa2dda624dac8a1a26fa4ea1e1451e13e`.
The nuspec source commit and existing Authoring27/Preview32 dependency pins are
verified before adoption. Windows shared control tests pass 14/14; SWLOR's
existing instance/facing/viewport regressions pass 47/47 in source and package
mode, zero skips. The SWLOR instance-row type moves to its own file without
changing its body. A pinned network-disabled Linux SDK 10.0.401 container builds
all four libraries and the test project with zero warnings/errors and passes the
nine portable camera/mesh/graph tests, zero skips. Its source is a Git archive of
the exact package commit, and its local package cache is hash-inventoried. Linux
rendering of the new form is not qualified; Windows remains the qualified
desktop target. Evidence is retained in Xenomech's
`artifacts/toolset/linux-instance-form-912bef3/` and `area-instance-form-*.log`.
Xenomech's final desktop package suite passes 80/80 and architecture checks
72/72, zero skips. Actual tag input retains keyboard focus across consecutive
edits; stale selection is refused, history and save/reopen retain the edits,
and trigger polygon resizing preserves unrelated native fields. The rendered
form is inspected in the full-width editor footer. This software-rendered UI
frame does not establish native GPU or official-client behavior. An independent
Docker inventory confirms cleanup of the owned Linux test container/network.
The SWLOR consumer commit is `a79f04b1e1f3fc90b0e2dbbcae433549bf59dc16`.

## Blueprint and placement property storage

The existing `BehaviorValueStore`, field/managed-value descriptors, storage/kind
and tag-scope enums, and choice models move from
`SWLOR.Toolset.Domain/Editors/Behaviors` at SWLOR source commit
`a79f04b1e1f3fc90b0e2dbbcae433549bf59dc16` into `Nwn.Authoring.Behaviors`.
They are the shared native property storage behind SWLOR's door, trigger and
waypoint area-instance editors. Six complete type bodies are compared against
the preceding Git source and match exactly. The existing choice enum and facet
record split into their own files; the choice class's existing `ResourceType`
alias expands to `ModuleResourceType`, with its body otherwise unchanged.

SWLOR retains its behavior catalogs, executable handler identifiers, palette
reader, key-item and self-closing-door rules, tag-index policy, and preview
services. No game/server/database dependency enters the shared authoring layer.
Xenomech binds the extracted store to its existing selected-instance transaction
and tag form; further behavior fields reuse this same storage responsibility.
The MIT notice remains in `licenses/SWLOR-MIT.txt`. Native strings, lists and
localized values are not reimplemented in a second editor.

All 64 preceding shared authoring tests pass after the move. Four further
regressions qualify transaction ownership and byte-identical undo, localized
string copying with all language/gender entries and the TLK reference, managed
value placement scope and declared clearing ownership, and refusal/rollback of
out-of-range native fields and resource references. The expanded Windows source
suite passes 68/68, zero skips. Both-host package and Linux qualification are
recorded after the immutable package train is built.
Xenomech's desktop source suite passes 80/80, zero skips, including actual
keyboard edits, selected-instance lifetime, undo/redo, and save/reopen using
the extracted store. The SWLOR corpus check exposes an outdated waypoint count:
content commit `c04b5adb77e87ead03c403e49dfd23b46677a1d4` adds the
`TATOOINE_TUSKEN_WARLORD` spawn waypoint in `tat_tuskcavebot.git.json`, after
the count last changed at `664fc76c2`. The exact Git hunk is inspected; the
expected spawn/total counts become 2123/4214, with an explicit assertion that
this waypoint classifies as a creature spawn. All other category expectations
and the complete dictionary comparison remain intact. The failed old-count
reports remain evidence, rather than being reported as passing runs.

The immutable local package train is Authoring `0.1.0-dev.28`, Preview
`0.1.0-dev.33` and Avalonia `0.1.0-dev.22`, built from source commit
`718e3beda1e16dea32d8026cfb77fa5da1df66c8`. Their SHA-256 hashes are respectively
`82d9e0fb6166deeaf2160d6d71fa08e003df9304b48b89a5c6a0beab016ff5d5`,
`178fab38c6359fe627ba4f5ed52b26e0faf5ba1a8d737e7ecc1b8b2401e79e10` and
`13b9b478d257d232c67c459341ecb9ae798bcb3bde4c633669cf54bff448da54`.
Formats remains `0.1.0-dev.25`. The nuspecs identify the exact source commit;
both hosts' lock files agree with the package archives' SHA-512 content hashes.
The packages are local and have not been published externally.

The selected existing SWLOR door/trigger/waypoint/property-editor regressions
pass 131/131 in both source and package mode. Two picture-catalog fixtures now
honor the explicit HAK corpus selector and fail when that required fixture is
missing, rather than silently ignoring it. The configured tests use the isolated
worktree's module corpus and the read-only SWLOR HAK corpus. Xenomech's desktop
suite passes 80/80 in source and package mode; its package authoring suite passes
139/139, ContentBuilder unit subset 43/43 and architecture suite 72/72. Every
reported final result has zero skips. The full Xenomech Release solution builds
with zero warnings/errors. These focused SWLOR and ContentBuilder results do not
represent complete application acceptance.

A fresh network-disabled, pinned Linux SDK 10.0.401 container builds all four
libraries from a Git archive of the exact package source with zero warnings/errors.
The complete authoring suite passes 68/68 and the portable control suite 9/9,
zero skips. All 35 cached package files match the retained hash inventory. The
recipe, source hash, cache inventory, build logs and reports are under Xenomech's
`artifacts/toolset/linux-property-store-718e3be/`. An independent label inventory
finds no remaining owned container or network. This proves portable storage and
control logic; Linux desktop rendering and native-client acceptance remain open.

## Native property rows and choice pickers

The existing `BehaviorRowViewModel`, choice model, responsive labeled-field panel,
gallery filter/sort models and row/search-picker XAML move from
`SWLOR.Toolset/Editors/Behaviors` at SWLOR source commit
`4d1405a0a944ed7f1b83ebcdf38cc5bad2378b1e` into
`Nwn.Toolset.Avalonia.Behaviors`. Four complete source files match the original
after namespace/import normalization. Gallery models split into one type per
file. The existing search debounce, page sizes, cancellation, stale-preview
handling, transaction hooks and field read/write pipeline remain in that same
implementation. The toolkit dependency remains pinned to SWLOR's existing
CommunityToolkit.Mvvm `8.4.2`.

`IBehaviorChoicePreviewProvider` lets each host retain its existing resource
lookup, artwork rendering and caches. SWLOR's concrete service implements this
contract; its two public image dimensions point to the extracted constants.
User-facing row/picker text moves to a complete typed catalog that a host can
replace, retaining the existing English captions and summaries. The two controls
explicitly preserve SWLOR's compiled bindings and identify the shared assembly.
This avoids the deferred search-template type-resolution failure exposed by the
first layout run after crossing the assembly boundary.

Xenomech supplies its native instance-field allowlist to these same controls:
waypoint map notes and existing door/area-transition-trigger destinations.
Ordinary triggers are not converted implicitly. Its existing transaction,
selection, preview coalescing and history remain authoritative. The host does
not implement another field widget or native-value writer.

Windows shared control tests pass 16/16, the selected existing SWLOR editor,
layout and preview regressions 132/132, Xenomech's complete desktop source suite
83/83 and architecture checks 72/72, zero skips. Three new desktop cases exercise
keyboard input, choice controls, undo/redo, stale-selection refusal, save/reopen,
localized map-note preservation, and unchanged native transition kind/geometry
and unknown fields. The rendered map-note form is inspected. Package and Linux
qualification follow the immutable source commit.

The broader SWLOR suite is not qualified: it hits animation-corpus and old
resource-error-message expectations, rejects an existing HAK entry named
`iprp_spells past`, and stalls for over seventeen minutes. Its exact owned test
process tree is stopped, with failure logs and process identity retained in
Xenomech's `artifacts/toolset/property-row-swlor-source-all.log` and
`property-row-swlor-stalled-run-processes.json`. The bounded selected run above
does not establish full application acceptance. Native archive compatibility,
complete transition authoring, appearance workflows, actual client behavior and
representative performance remain separate gates.

The immutable local Avalonia `0.1.0-dev.23` package is built from shared source
`ee3d3ca90285931bf71c6b0e661a154a944ff132`, with SHA-256
`615ec45cb81db506b79535242bb4442e9c44851c641d67f0232903f87bd44317`.
Authoring28, Preview33 and Formats25 retain their preceding immutable packages.
Both hosts pass the same regressions in package mode: Xenomech 83/83 and the
selected SWLOR suite 132/132, zero skips. Nine changed host lock files contain
only Avalonia23, its existing SWLOR MVVM dependency and corresponding dependency
references. Package content hashes are checked using the actual archives;
NuGet's content-hash API verifies the signed MVVM archive. Xenomech's complete
Release solution builds with zero warnings/errors.

A Git archive of this exact source builds all four shared libraries in the
pinned, network-disabled Linux SDK 10.0.401 image with zero warnings/errors.
Authoring passes 68/68 and portable control logic 10/10, zero skips, including
the replaceable typed property-row catalog. The 36-file offline dependency
inventory, source hash, logs, reports and ownership inspection are retained in
Xenomech's `artifacts/toolset/linux-property-row-ee3d3ca/`. Its completed
run-labeled container is removed after verifying ownership, no network and no
port bindings; an independent inventory finds no remaining container. Linux
desktop rendering and native-client behavior are not established by these
portable tests. No package is published externally.

## Legacy native archive keys and bounded stock reads

The explicitly assigned SWLOR native stack contains 119 packed HAKs, 177,322
entries and 920 noncanonical keys. An independent raw key-table inventory finds
spaces, ampersands, apostrophes, hyphens, periods and a tilde; rejecting the first
space-bearing key prevents startup and also hides valid tileset/art resources.
SWLOR's previous `HakArchiveCatalog` at `0058013a9^` preserves these names.
It is evidence for the read contract, not source copied into this implementation.

`Resref.Parse` and `TryParse` keep the existing canonical authored-name policy.
Only the internal native archive-key reader accepts bounded printable ASCII,
normalizes case and retains punctuation. Control bytes, non-ASCII bytes, empty
names, malformed indexes and invalid resource ranges still fail. Imported typed
keys and payloads survive streaming repack, without silently skipping or renaming
them. The neutral resolver retains their actual archive/entry provenance and
per-resource read limits.

SWLOR's existing allocation regression also exposes a distinct shared stock-read
bug: a 1 KiB resource request loads an entire 8 MiB BIF before refusing its payload.
`BifReader` now has one metadata parser for stream and byte-array callers.
`StockArchive.FromStreams` retains bounded metadata, disposes each caller-supplied
stream, checks source-length drift, and reads only the requested bounded payload.
The explicit byte-array constructor continues to retain caller-supplied snapshots.
The neutral KEY/BIF resource layer and SWLOR install adapter use the stream path;
configured source-file, resource and retained-index limits remain enforced.

Shared formats pass 159/159 and authoring 69/69 with their explicit read-only
native corpus selectors, zero skips. The selected SWLOR native resource suite
passes 24/24, including all assigned archives, four independently calculated
winning payload hashes/provenance checks, preallocation limits, layer precedence
and a visible cold-start creature gallery. Existing bounds assertions identify
the numeric limit and limit refusal without requiring the former decorative
`-byte` error wording; the allocation and no-fallback checks remain unchanged.
New stream-source tests establish metadata-only rejection, stream disposal,
retained-index budgeting and source-length drift refusal.

Missing-selector, allocation and aborted test-fixture failures remain separate
artifacts, not passing evidence. The complete SWLOR suite, animation-corpus
qualification, both official clients and representative performance remain open.
Both-host immutable package and Linux qualification are recorded after the
source is committed and packaged.

The immutable local train Formats26, Authoring29, Preview34 and Avalonia24 is
built from exact shared source `38e2cd076c5ee656054505b862473fbdf8af72ef`.
Their SHA-256 hashes, respectively, are
`7710ac601f4e1d10698c055e0862adb4962fe6b5675ce13a9ea24c4fed73f122`,
`0e351e3221e25ccdbab41c1af8e699fae7446fff4454d22603193fa9ceb02c7f`,
`64c34c747876ef5c7317e4375aee8740f5a0e3a6d1c47a59e6ac9cbf5807f2f2`,
and `2d973ec29c515af4364c00e78dfbf56597cd045c732566cebab47cc62f6e68c0`.
All nuspecs name that commit; package dependency graphs and archive hashes match
the thirteen Xenomech and nine SWLOR lock files. No other dependency changes.

Both consumers also pass in package mode: affected SWLOR native resources
24/24, Xenomech formats 112/112, stock extraction 5/5, desktop 83/83,
authoring 139/139 and architecture 72/72, zero skips. The actual Xenomech stock
extractor now opens streams; its regression extracts one table from a 64 MiB
archive while allocating less than 2 MiB. The complete Release solution builds
with zero warnings/errors. Sandbox-denied PostgreSQL fixture startup and
SWLOR's build-telemetry file access are retained unsuccessful runs; the final
qualified commands have the required isolated Docker access and disable only
the unrelated build telemetry target.

An exact Git archive of the shared source builds all four libraries in the
pinned, network-disabled Linux SDK 10.0.401 image. Formats pass 159/159 against
both explicitly selected native corpora, authoring 69/69 and portable controls
10/10, zero skips. Builds have zero warnings/errors. Source archive SHA-256 is
`06448300d4e4251c574a9a263bae49749db998b223569d68dad88ea18ab942c5`.
The completed run-owned container is removed after verifying its exit, labels,
network and port configuration; an independent label inventory is empty.
Xenomech's `artifacts/toolset/legacy-archive-handoff-verification.json` records
every report's outcomes, hash, package graph and lock-file verification. Its
Linux recipe and ownership evidence live in `linux-legacy-archives-38e2cd0/`.
These checks close the legacy-key and stock-read allocation regressions, while
the broader animation corpus and official-client acceptance remain open.

The shipped `xp3.bif` is 683,611,953 bytes, above the shared conservative
source-file default. Host policy changes in Xenomech `375b80b0f` and SWLOR
`6ead9c547` explicitly permit source BIFs up to 1 GiB while preserving metadata
and payload allocation limits. Shared defaults and immutable packages remain
unchanged. Both actual host adapters read the independently hashed 62,201-byte
`tbw01.set` payload, refuse a 62,200-byte limit and allocate less than 4 MiB.

SWLOR's asset checkout is isolated and clean at its exact gitlink
`3ec67b47d533c06773a86596d917999029915db9`, with 177,437 tracked files. With this
fixture and explicit native installation, its broader resource/appearance suite
passes 476/476 with zero skips. Xenomech's full authoring suite passes 140/140,
architecture 72/72 and stock workspaces 7/7 on Windows and offline Linux, zero
skips. The Linux source is exact commit `375b80b0f`; its eighteen-package cache,
source hash, read-only corpus mount and verified container cleanup are retained
under Xenomech's `artifacts/toolset/linux-stock-workspace-375b80b0/`.
`large-stock-handoff-verification.json` checks every final test outcome and hash.
The aggregate animation preparation timeout is retained as an aborted run.
SWLOR `a8b959bb7` exposes each registered clip as a named test case with the same
assertions and timeout budget. The final animation draft/fidelity report passes
348/348 with zero skips, including all 284 registered clips against the compiled
banks and installed male/female motion fidelity. The verification artifact
`pinned-animation-handoff-verification.json` checks every outcome, every registry
case name, the source/report hashes and clean asset pin. Production animation
behavior is unchanged; native/official-client acceptance remains open.

## Native segmented-body follow-up

The native 18-field body/armor/attachment mapping moves from SWLOR's
`BlueprintModelResolver` at `a8b959bb75fd4243bbc1839770ba34e47d3d0735` into
`Nwn.Authoring.Appearances`. The tuple values and order remain unchanged;
`CreatureBodyPartField` names the existing tuple and the catalogue is read-only.
The native right-foot field remains `ArmorPart_RFoot`. SWLOR's existing creature
and armor selection loops consume this same mapping.

`ComposedPartTextures` moves from SWLOR's domain rendering folder into
`Nwn.Preview.Scene`. Its complete type body is unchanged: texture recording,
NULL sentinel handling, authored-texture restoration and case-insensitive mesh
identity retain their implementation. The SWLOR renderer consumes the moved type;
its old source file is removed. Existing texture regressions remain in SWLOR and
also run with MSTest in the shared repository.

Xenomech's area integration uses the existing `MdlPartComposer`, geometry
flattener, idle sampler, mesh builder and creature-facing correction. Configured
native Standard bare-body fields select the parts; no default body is invented
for incomplete blueprints. Equipped overlays and unsupported phenotypes retain
the marker fallback until their integration qualifies. Game appearance tables,
compiled species metadata and database authoring remain host responsibilities.
Source reads and the retained composed-model cache are bounded.

Source-reference checks pass: shared authoring 70/70, shared composition 4/4,
Xenomech desktop 99/99 and affected SWLOR 51/51, zero skips. The Xenomech suite
includes all six actual species/sex fits using compiled metadata and licensed
native/model content. These are offline/editor proofs, not native-engine or
official-client appearance acceptance.

The immutable local train retains Formats26 and adopts Authoring30, Preview35
and Avalonia25 from exact source `fef88fc3783c5d952275af648b97bb9089a74004`.
Both real package consumers pass the same checks: Xenomech desktop 99/99 and
affected SWLOR 51/51, zero skips. Package hashes, dependency graphs, twenty-two
consumer lock files and eight copied consumer DLLs match. Existing historical
fixture locks remain unchanged. Xenomech architecture passes 72/72.

A fresh Git archive of that source also passes authoring 70/70 and composition
4/4 on offline Linux SDK 10.0.401, zero skips and zero build warnings/errors.
The archive SHA-256 is
`bfb09eca3e74285cb92a8b6b925c78b3e8a1eb2039962874034bfbbe950d3943`;
fourteen offline package hashes are retained. The container has no network or
ports and is removed after exact owner/run/exit checks; independent cleanup
inventory is empty. Xenomech's
`artifacts/toolset/segmented-appearance-handoff-verification.json` records the
source-provenance, package, consumer and Windows/Linux report evidence.
Native/official-client behavior and equipped-overlay integration remain open.

## Shared native local-variable editing

`VarTableSectionViewModel`, `VarTableRow`, and `VarTableSectionView` now live in
`Nwn.Toolset.Avalonia.Variables`. The view model edits the existing
`Nwn.Authoring.Documents.Native.VarTable` and accepts the owner’s `Func<string,
Action, bool>` transaction runner, so it does not replace native storage or undo
semantics. Typed `VarTableStringId` values and the embedded English catalogue
supply captions and validation text. Optional key suggestions, filtering, and
value hints are neutral inputs; the shared package has no game-code dependency.

SWLOR keeps its three known-key suggestions and NPC-group validation in its host
policy adapter. Xenomech binds the same editor to the selected GIT instance and
`AreaDocumentEditSession.ExecuteInstances`; the desktop places it beside the
existing instance property rows. Undo restores the document snapshot and the
selection refresh creates a new editor for the restored native struct. Saving
and reopening retain the new local and unrelated unknown GIT fields.

Only `Nwn.Toolset.Avalonia` advanced for this work, to internal dev.30. Both
hosts retain Formats26, Authoring30 and Preview35. Both hosts pin Avalonia30; the
Xenomech desktop and its test projects resolve the same package in their Debug
and Release locks. The package remains local to the recovered feed.

### Minimum-width instance fields

The extracted coordinate/facing/geometry controls wrap their existing label and
number pairs when the host pane narrows. The native-local editor uses separate
name/type and value/action rows so its inputs remain usable in a 330-pixel pane.
Its bindings and owning edit transactions are unchanged; undo descriptions and
unknown-type labels use the shared text catalogue. The two focused shared
control tests pass 2/2 with zero skips in
`tests/Nwn.Toolset.Avalonia.Tests/TestResults/shared-field-layout-dev31.trx`.
A fresh local Avalonia `0.1.0-dev.31` package carries this layout correction;
older versions are preserved.

## Shared Scene/Properties layout

SWLOR source `72fa44b51` has a Scene/Properties TabControl in
`SWLOR.Toolset/Editors/Views/AreaEditorView.axaml`. That neutral layout
moves to `Nwn.Toolset.Avalonia/Areas/AreaEditorLayout.cs`. Each host
supplies its existing scene and property contents; SWLOR retains its context
menus, camera pad, view-state binding, catalogs and per-resource forms.
The shared control keeps the scene's full content area when properties change
and preserves the same content controls through tab switches. Labels use a
host-replaceable typed text catalogue. This is a layout extraction, not a second
viewport, tile solver or property implementation.

The shared suite now loads the same pinned Fluent control theme as Xenomech's
desktop fixture. Complete shared controls pass 18/18 with zero skips in
`tests/Nwn.Toolset.Avalonia.Tests/TestResults/shared-area-layout-fluent-qualified-dev32.trx`.
The initial fixture constructed a control outside its UI dispatcher and was
aborted after its failed initialization; subsequent missing-theme attempts
remain failed reports. Neither is passing evidence. Actual both-host package
adoption, full-window rendering and official-client evidence follow separately.

### Both-host layout and native-grid qualification (2026-10-03)

Avalonia dev.32 was packed once from production source
`d97cb90f35b4597db42a0a773046ec07e97e773a`. Its SHA-256 is
`F5AE982E326559DB356E1CBDA587561A44CC3CE8811508902163BE7794E649A5`.
Both applications adopt the shared Scene/Properties control with their existing
scene and property contents. SWLOR's actual area view and affected workflows pass
20/20, zero skips, in its
`SWLOR.Toolset.Tests/TestResults/swlor-shared-scene-properties-dev32.trx`.
Xenomech's affected area tests pass 11/11, its unit/database tests 116/116,
architecture 72/72 and real OpenGL viewport checks 5/5, all zero skips, under
`artifacts/toolset/shared-area-layout`.

Rendered-frame inspection found Xenomech had omitted the DataGrid Fluent style
that SWLOR already loads. Both the Xenomech host and UI fixture now load it.
The shared test fixture also loads that style, and asserts an actual rendered
DataGridRow and selection forwarding to the existing native-variable editor.
The complete shared-control suite passes 18/18, zero skips, in
`tests/Nwn.Toolset.Avalonia.Tests/TestResults/shared-area-layout-vartable-themed-dev32.trx`.
This is a test-fixture correction after the package was produced; dev.32 is not
repacked. The native-grid fix is a host integration change.

These results establish shared source consumption and the tested UI behaviors.
The Xenomech application shell, styling, database workflows and catalogs remain
host-specific. A headless property-pane frame does not establish complete SWLOR
visual parity, equipped-creature previews or official-client acceptance.

## Shared camera pad

The pan/orbit/zoom/reorient buttons and their eleven handlers move from SWLOR
`01052fc1f` into `Nwn.Toolset.Avalonia.Areas.AreaCameraControls`.
Original glyphs, directions, existing viewport methods, 16ms repeat cadence and
26×24 sizing are retained. Tooltips use the host-replaceable area string
catalogue. Narrow hosts wrap the control; camera mathematics and document editing
remain unchanged. Object rotation and tile-height commands remain in their
existing host/editing integrations.

The shared fixture exercises actual button clicks against the existing viewport,
checks orbit/zoom/reset, preserves the scene instance and verifies all eleven
actions remain on-screen at width 330. Pan needs a real GL viewport height and must be
qualified in the Xenomech native viewport integration; it cannot be inferred from
a headless zero-height viewport. The variable fixture keeps its bound editing
transactions on the UI dispatcher; its earlier off-dispatcher mutation attempt
remains a failed report.

Shared controls pass 19/19, zero skips, in
`tests/Nwn.Toolset.Avalonia.Tests/TestResults/shared-camera-controls-dispatcher-qualified-dev33.trx`.
The fixture settles dispatcher/layout/render work before frame assertions and
keeps bound variable edits on that dispatcher. Earlier zero-height pan and
unsettled graph-frame attempts remain failed reports. Both-host adoption and the
real GPU camera-button proof remain separate until their integration runs pass.


## Shared complete scene surface and manipulation pad

The scene Grid from SWLOR source 514a8deae now lives in
Nwn.Toolset.Avalonia.Areas.AreaSceneView. The existing map surface, loading and
render failures, selection/placement hints, drag coordinate readout, and floating
camera, rotation and tile-height pad remain together. The scene uses the original
SWLOR palette and typography locally so a host without ToolsetTheme still supplies
the same scene resources. The narrow pad wraps without reducing the map's height.
The original rotate preview repeats while held and commits exactly once on release
or capture loss. Random rotation delegates to the existing viewport event; tile
height actions delegate to each host's existing edit transaction.

SWLOR removes these view handlers and drag projection fields from its existing
view/model and adopts AreaSceneView while retaining its context menu, property
controls, document state, selection, navigation and undo ownership. Xenomech binds
its existing transaction adapters to that same Surface and Overlay. Loaded-scene
errors remain visible in the scene HUD; ordinary saved/loaded captions stay in
Properties. Programmatic tile selection keeps the viewport highlight and height
commands synchronized with the same editor state.

Source qualification: complete shared controls 21/21 zero skips in
shared-scene-control-themed.trx; affected SWLOR source workflows 116/116 zero skips
in area-scene-control-source-final.trx; Xenomech related UI and actual database/
OpenGL AreaEditorPanel workflow 11/11 zero skips in
xeno-shared-scene-notices-qualified.trx. The routed UI tests establish click event
wiring and capture-loss behavior; the failed SWLOR headless physical mouse attempt
remains separate. The bare-viewport image is renderer evidence, not a capture of
the complete scene or application. Complete shell/sidebar parity and official
client acceptance remain open. New packages use unused Authoring dev.31 and
Avalonia dev.34; locked adoption and hashes are recorded after packaging.


## Shared selection menu and Area Contents

The selection captions, Open properties, Edit blueprint and Edit Copy menu from
SWLOR's existing AreaEditorView now live in AreaSelectionContextMenu. Observable
selection and command availability come from each host; the original SWLOR
commands remain authoritative. Empty-ground suppression stays with the host.

The existing Area Contents grouping/filter/tree and its view move together to
Areas/Contents. Name, Blueprint, Tag and Flat grouping, the 200-member overflow
row, exact-instance reveal, context properties, framing and deletion remain the
same workflows. Immutable snapshots carry native resource kind, instance index,
resolved name, resref, tag and position; host actions own document history, native
resolution and destructive confirmation. The SWLOR Dock Tool retains only those
adapters; its duplicate grouping/node implementations are removed. Xenomech
replaces its flat instance list with this shared view.

The complete shared Release source suite passes 26/26 with zero skips in
SharedAvalonia-Release-full-final.trx. The mounted-tree check establishes the
compiled command binding and parameter; it executes the bound command and does
not establish physical mouse input. Earlier incorrect routed-event simulations
and aborted test reports remain retained. The original complete SceneView and
renderer implementation are unchanged by this extraction.

## Door appearance storage mapping

The native door appearance field mapping comes from SWLOR's
SWLOR.Toolset.Domain/Editors/Doors/DoorValueStore.cs at source commit
cea6099733c484d0c7a3119fe18f590a444f08b5. Nwn.Authoring.Doors now owns
the neutral kind, stored identifier and read/write mapping over
BehaviorValueStore. A positive Appearance selects the specific table;
otherwise GenericType_New takes precedence over legacy GenericType, even
when its value is zero. Writes update only Appearance and
GenericType_New, preserving the legacy field and unknown GFF data. SWLOR
continues to own its game tables, display names, preview models and choice
policy.

## Area instance clipboard

SWLOR's session clipboard moves from `SWLOR.Toolset/Editors/AreaInstanceClipboard.cs`
at commit `fae91485822ed17ca96b136abe890d455e894faa` into
`Nwn.Preview/Areas/Clipboard`. The clipboard and its entry each have a separate
file. Their native instance, aligned GIC comment and existing scene marker remain
unchanged; the entry uses the shared `ModuleResourceType` identity. Public access
permits both desktop hosts to share one clipboard across their open area editors.

Each host captures independent native clones and uses the existing shared instance
editing transactions for placement and undo. Module identity and door socket
validation remain part of the host's existing placement workflow. SWLOR's former
clipboard type is removed when its callers adopt the shared package. Xenomech's
integration and both-host workflow qualification remain open until their actual
copy, cursor placement, save/reopen and undo checks pass.

The shared category catalog and native ARE field catalog are extracted from the existing SWLOR desktop models and AreSchema. SWLOR retains ITP/TLK seeding and host classification, and projects its ARE editor labels from the shared schema. Xenomech keeps database definition authority in its own adapter. Category JSON remains toolset metadata beside the module, containing names and membership rather than game definitions.

## Shared palette workflow

SWLOR's palette workflow moves from `SWLOR.Toolset/Shell/Panels/PaletteViewModel.cs`
at commit `6560e2f1f5fd835f298ab7e5ab3f88412900bde9` into
`Nwn.Toolset.Avalonia/Palettes/Workflow`. `PaletteWorkflowController` implements
`IPaletteActions` and owns snapshot projection from category sections (folders,
path-keyed pins, Unsorted, existing-only counts), type/source/mode switching with
saved preferences, Standard versus Custom loading, the category commands (new,
rename, delete, pin, file selected entry), the entry commands (place, edit, Edit
Copy, delete, create), preview orchestration, tile mode and stale-entry refusal.
`PaletteCategoryCommands` and `PaletteEntryCommands` hold the two command groups.
All text is `PaletteWorkflowStringId`/`PaletteWorkflowTexts` from
`PaletteWorkflowEnglish.json`; the existing `PaletteTexts` tile messages are reused.

The generic ITP import moves from `SWLOR.Toolset.Domain/Categories` into
`Nwn.Authoring/Categories`: `ItpCategoryImporter` (JSON `ItpDocument` and native
`GffDocument` overloads, `ItpImportLimits` depth/node bounds),
`StandardPaletteReader`, `StandardPalette`, `CategoryPlaceholderRepair` and
`CategoryPlaceholderNames`. `ItpDocument`/`PaletteNode` move to
`Nwn.Authoring/Documents/Native`. Palette resource names stay host data: SWLOR's
`*palstd`/`*palcus` names remain in `StandardPaletteLoader` and `CategoryService`.

Hosts supply `PaletteWorkflowHost`; only the first two members are required.

| Interface | Members | SWLOR supplies | Xenomech supplies |
|---|---|---|---|
| `IPaletteContentSource` | `OfferedTypes`, `IsModuleOpen`, `CustomResRefs`, `CustomName`, `Standard`, `PluralName`, `SingularName` | Module JSON files and catalog names; `*palstd` ITPs through `ResourceIndex` and TLK | Database UTC/UTP/UTI definitions plus native UTD/UTM/UTS/UTT/UTW files; `NativePaletteCatalogLoader` standard import; `DesktopStrings` type names |
| `IPaletteCategoryStore` | `Changed`, `Section`, `CanSaveChanges`, `SaveChanges` | `CategoryService` (module write lock, mtime+hash conflict check, `itp/*palcus.itp.json` seeding, TLK repair) | `AreaPaletteCategoryCatalog` sidecar with `CategorySidecarSaveLock` and native-organization write adoption |
| `IPaletteBlueprintOperations` (+ `IPaletteBlueprintDeletion`) | `CanCreate`, `CanDelete`, `Create`, `Copy`, `PrepareDelete`, `IsOpenInEditor`, `OpenEditor`; deletion `DisplayName`, `Location`, `IsCurrent`, `Commit`, `Dispose` | Template/copy factories, `SwlorFileWriteAccess`, SHA-256 fingerprint, mutation lock and module write lease held until disposal | Definition/native editors and copy; a `Create` adapter over its creation dialog; `CanDelete` false until it supports deletion |
| `IPalettePrompts` | `PromptForTextAsync`, `ConfirmDestructiveAsync` | `IEditorPromptService` | `PaletteCategoryDialogService` dialogs |
| `IPalettePreviewSource` | `Invalidated`, `IsAvailable`, `TypeIcon`, `LoadBlueprintAsync`, `LoadTileAsync` | `ThumbnailService` | Its palette preview service |
| `IPalettePlacementTargetProvider` / `IPalettePlacementTarget` | `ActiveTarget`; `TilesetResRef`, `ArmPlacement`, `ArmTilePlacement` | Active area document from the dock factory | `AreaEditorPanel` placement and tile arming |
| `IPaletteTilesetSource` | `Load` -> `PaletteTileset` | `TilesetCatalog` + `TilePaletteBuilder` | Its prepared tile palette |
| `IPaletteSettings` | `PreviewSize`, `CategoryProportion`, `Selection`, `Source`, `TilePaintMode` | Existing `ToolsetSettings` keys | Its desktop settings |
| `IPaletteWriteGate` | `Changed`, `IsLocked` | `ModuleMutationLock` | Busy/disposed/area-active gate |
| `IPaletteLog` | `Write` | Output panel | `IGameLog` |

SWLOR's behaviour is the default. Xenomech-only features: the Automatic/Folders/Flat
grouping selector is not supported by the controller, because SWLOR always shows the
stored folder tree and honouring `CategorySection.Grouping` would change SWLOR's
visible palette; it stays a candidate for an opt-in projection. Cross-editor sidecar
adoption is supported as a store policy inside Xenomech's `IPaletteCategoryStore`.
ITP depth/node limits are shared (`ItpImportLimits`); the byte-size limit stays in the
host's resource read. Xenomech's resource-source subtitles are not supported; entry
subtitles are resrefs as in SWLOR.

SWLOR keeps `PaletteViewModel` as the Dock tool, forwarding its existing public
members to the controller, with one adapter per interface under
`SWLOR.Toolset/Shell/Panels/PaletteHost`. Xenomech adoption replaces the palette
section of `AreaEditorPanel` (snapshot building and every `IPaletteActions` member)
with a `PaletteWorkflowController`; `AreaPaletteCategoryCoordinator` and the
projection half of `AreaPaletteCategoryCatalog` are deleted, its persistence half
becomes the `IPaletteCategoryStore` adapter, `NativePaletteCategoryImporter` and its
import records are replaced by `ItpCategoryImporter`/`StandardPaletteReader`, and the
`PaletteCategory*` strings covered by `PaletteWorkflowTexts` are removed.

## Shared Module Contents explorer

SWLOR's Module Contents panel moves from `SWLOR.Toolset/Shell/Panels/ModuleExplorerViewModel.cs`
(with `ExplorerNodeViewModel`, `ExplorerTabViewModel`, `ExplorerItem`, `FolderTargetViewModel` and
`Shell/Views/ModuleExplorerView.axaml[.cs]`) at commit `6a0c75e3c64eca5f24fed747887f603581f9abd3`
into `Nwn.Toolset.Avalonia/Explorer`. `ModuleExplorerController` (`Explorer/Workflow`) owns the tab
model with counts and the persisted selected tab; the folder tree projected from each
`CategorySection` (pinned first, then alphabetical; the always-present Unsorted bucket; descendant
counts; dead-end folders hidden while searching; expansion kept by name across rebuilds; rows
published flat, only beneath expanded folders); name/resref search with an optional debounced content
search; one-time folder seeding through the host; folder create, rename and delete (a non-empty folder
is confirmed and its members return to Unsorted); "Move to" and "Remove from folder"; drag-drop
decisions (`CanDropResource`/`DropResource`); move undo/redo; creation that files into the folder
captured when "New ..." was clicked; and the confirmed delete flow. `ExplorerTreeBuilder` and
`ExplorerContentSearchCoordinator` hold the projection and the search. The view is
`Explorer/Views/ModuleExplorerView`; its markup and pointer/keyboard code-behind are unchanged apart
from bindings. All text is `ModuleExplorerStringId`/`ModuleExplorerTexts` from
`ModuleExplorerEnglish.json`. The non-UI move logic is in `Nwn.Authoring/Categories`:
`CategoryMembershipEdit`, `CategoryMembershipHistory` (undo/redo; an edit leaves its stack only after
its replay is saved) and `CategoryMembershipMoves` (move, resolve stored path keys, apply).

Hosts supply `ModuleExplorerHost`; only the first two members are required. Prompts, the category
store, the write gate and the log are the palette's interfaces, because both panels edit the same
sidecar under the same module lock: a host implements each once and passes the same adapter to both.

| Interface | Members | SWLOR supplies | Xenomech supplies |
|---|---|---|---|
| `IModuleExplorerContentSource` | `ResourceChanged`, `ContentChanged`, `Sections` (`ExplorerSection`: type, label, singular label, `IsCompilable`), `IsModuleOpen`, `Items`, `Count`, `Exists` | Areas from module JSON with `BlueprintCatalog` names; Dialogs = conversation graphs plus legacy DLGs minus generated shells; Scripts (compilable); `WorkspaceContext` catalog events | Areas from the flat module source root with `AreaSourceDocumentNames`; Scripts from `*.nss` in the source root; no Dialogs section (conversations are database-owned and open in its DB editor) |
| `IPaletteCategoryStore` | `Section`, `CanSaveChanges`, `SaveChanges` (`Changed` is not subscribed; hosts call `Refresh`) | `CategoryService` through `SwlorPaletteCategoryStore` | `NativeSourceCategoryCatalog` persistence with `CategorySidecarSaveLock`, external-change detection and owned-write adoption |
| `IModuleExplorerOrganization` (optional) | `IsReadyToSeed`, `Seed`, `LeafLabel` | `ModuleFolderSeeder` naming rules; areas wait for catalog names | None, or its own rules; without it nothing is seeded and rows show their names |
| `IModuleExplorerContentSearch` + `IModuleExplorerContentSearchScan` (optional) | `Supports`, `SearchingLabel`, `FailureMessage`, `Prepare` (UI thread, snapshots open editors); scan `Run` (worker, cancellable) | `DialogueSearch` over DLGs and graphs with open-editor snapshots | None |
| `IModuleExplorerCreation` (optional) | `CanCreate`, `Mode` (`NamePrompt`/`Form`), `ToResRef`, `Exists`, `ChooseOptionsAsync`, `Create`, `OpenForm` (`ModuleExplorerFormCallbacks`), `Created` | `NewAreaViewModel` as the area form; script and conversation-graph templates through `SwlorFileWriteAccess`; catalog refresh and placement-index invalidation | Its `AreaCreationFormState` as the area form, completing through its area creation; a script template writer for `NamePrompt` |
| `IModuleExplorerEditors` (optional) | `CanOpen`, `Open`, `IsOpen`, `IsModulePropertiesOpen`, `TryCloseForDeletion`, `CompileAsync` | `EditorService` | Its document dock: area and script panels, the module properties command, forgetting committed-deletion documents, its compiler |
| `IModuleExplorerDeletion` + `IModuleExplorerPreparedDeletion` (optional) | `CanDelete`, `Prepare`, `TryReserve`, `Deleted`; prepared `Commit` | `ModuleResourceDeletionService` under `ModuleMutationLock.TryBeginResourceDeletion` and `AllowModuleWrites` | `NativeSourceDeletionService.Prepare`/`Commit`; its shell-operation busy gate as the reservation |
| `IModuleExplorerSelection` (optional) | `Selected` | Properties panel | Its properties panel, or none |
| `IModuleExplorerSettings` (optional) | `SelectedSection` | Existing `moduleContentsTab` key (resource extension) | Its desktop settings |
| `IPalettePrompts`, `IPaletteWriteGate`, `IPaletteLog` (optional) | As in the palette section | `IEditorPromptService`, `ModuleMutationLock`, Output panel | Its dialogs, busy/disposed gate, `IGameLog` |

SWLOR's behaviour is the default and is unchanged, including its settings key and sidecar. Invisible
differences: a name-prompt creation captures its folder when "New ..." is clicked rather than after
the modal prompts; the Unsorted row is identified by a flag rather than by its name; content-search
results are cached per section and query; the conversation-graph root is read when the scan is
prepared rather than on the worker. SWLOR keeps `ModuleExplorerViewModel` as the Dock tool forwarding
its public members to the controller, a nine-line `ModuleExplorerView` hosting the shared view,
`NewAreaViewModel`, and one adapter per interface under `SWLOR.Toolset/Shell/Panels/ExplorerHost`,
reusing the palette's prompt, store, write-gate and log adapters. `ModuleResourceDeletionService`,
`DialogueSearch`, `ModuleResourceTemplateFactory`, `ConversationGraphTemplateFactory` and
`ModuleFolderSeeder` stay SWLOR's.

Not supported, because SWLOR does not have them: Xenomech's "All scripts" pseudo-folder, the Reload
categories command, highlighting the active script, and deleting only empty folders (the shared panel
confirms and returns members to Unsorted). Script creation becomes a name prompt instead of a file
picker. Xenomech's deletion dialog listing the prepared files is reached only through
`IPalettePrompts.ConfirmDestructiveAsync`; showing the file list needs its prompt adapter to read it
from the deletion adapter's last prepared plan.

Xenomech adoption plan (`tools/Xenomech.Toolset.Desktop`):

- Delete `Areas/Explorer/AreaSourceExplorer.cs`, `AreaSourceExplorerEntry.cs`, `AreaSourceFolderTarget.cs`,
  `Scripts/Explorer/ScriptSourceExplorer.cs`, `ScriptSourceExplorerControl.cs`,
  `ScriptSourceExplorerLabels.cs` and `ScriptSourceFolderSelection.cs`; the controller, the shared view,
  `ExplorerItem` and `ExplorerMoveTarget` replace them.
- Replace `Modules/ModuleExplorerView.cs` with the shared `ModuleExplorerView` bound to a
  `ModuleExplorerController`. Its flat native blueprint list (UTD/UTM/UTS/UTT/UTW) is not a Module
  Contents section in SWLOR's model, because those types are the palette's: drop it once the palette
  workflow is adopted, or add one `ExplorerSection` per type, in which case
  `Modules/NativeModuleResourceNames.cs` stays as their name resolver; otherwise delete it.
- Keep `Areas/Explorer/AreaSourceDocumentNames.cs` as the area-name reader behind a new
  `IModuleExplorerContentSource` adapter (sections Area and Nss).
- Reduce `Modules/Organization/NativeSourceCategoryCatalog.cs` to the `IPaletteCategoryStore` adapter:
  keep loading, `CanMutate` (file identity, read-only, external change, `AdoptKnownNativeOrganizationWrite`),
  saving and `Reload`; delete `CreateFolder`, `RenameFolder`, `DeleteEmptyFolder`, `Move`,
  `RemoveMembership`, `FolderTargets`, `Members` and `UnsortedMembers` and their operation and outcome
  types once unused. Keep `CategorySidecarSaveLock.cs`.
- In `Shell/ToolsetWindow.cs`, build the host and controller where `ModuleExplorerView` is constructed;
  move `DeleteNativeSourceCoreAsync`'s orchestration (busy state, module-properties checks,
  confirmation, stale and busy refusals, category cleanup, status text) to the controller, keeping the
  `NativeSourceDeletionService` calls in the deletion adapter and document forgetting/replacement in
  the editors adapter's `TryCloseForDeletion`; replace `CreateAreaDocumentAsync`'s captured-folder
  filing and the script create path's `CaptureSelectedFolderForNewScript`/`FileNewScriptIntoCapturedFolder`
  with `IModuleExplorerCreation`; remove `SetActiveScript`.
- Remove the `AreaSource*`, `ScriptSource*` and `ModuleResource*` desktop strings covered by
  `ModuleExplorerTexts`, or pass a `ModuleExplorerTexts` built from its catalogue.
- `src/Xenomech.Authoring/NativeSources/NativeSourceDeletionService.cs` is unchanged.

## Shared area Properties page

SWLOR's area Properties page moves from SWLOR source commit `6c40c149e229fcd713af0744ba43ac4760d81004`
into the shared libraries: the page layout from `SWLOR.Toolset/Editors/Views/AreaEditorView.axaml`, the
area-property field construction from `AreaEditorViewModel`/`AreSchema`, the instance sections from
`InstanceListSectionViewModel`, `InstanceRow`, `PaletteBrowserViewModel` and `PaletteNodeViewModel`, the
schema field editors from `FieldViewModels.cs`/`FieldViewModelFactory.cs` and the eight field templates in
`App.axaml`, and the typed door, waypoint and sound editors (`Editors/Doors`, `Editors/Waypoints`,
`Editors/Sounds`, their views, rows and the behavior rail/header in `Editors/Behaviors`). First-party SWLOR
source remains MIT, copyright 2019 Zunath, retained in `licenses/SWLOR-MIT.txt`. SWLOR's behaviour and look
are the reference: markup is copied, captions become typed text catalogues with SWLOR's English as the
embedded default, and game-specific data stays in the host.

Where the code now lives:

- `Nwn.Authoring/Fields`: `FieldDescriptor`, `FieldGroup`, `EditorKind`, `SchemaFieldAccessor`,
  `FieldUnsetSentinel` (SWLOR's `DropdownValueValidator.GetUnsetSentinel` delegates to it).
- `Nwn.Authoring/Behaviors`: `IBehaviorDescriptor`, `BehaviorSwitchLosses`, and the transition-destination
  contract: `TransitionDestinationStatus` (Resolved, NotFound, Ambiguous, WrongType, CatalogIncomplete,
  TypeUnset, TypeNone), `TransitionDestinationResult` (status plus the resolved description, match count or
  the kind actually found; `IsGood`, `FromLocation`), the `TransitionDestinationResolver` delegate a host
  supplies, and `TransitionDestinationFlags` (the native `LinkedTo`/`LinkedToFlags` names and the
  1 = door, 2 = waypoint encoding).
- `Nwn.Authoring/Doors`: `DoorBehavior` (new `KeyRequiredRule`), `DoorFieldDefinition`, `DoorFieldSpecial`,
  `DoorKeyRequiredRule`, `DoorAppearanceChoice`, `DoorScriptConventions`, `DoorBehaviorValueStore` (the
  generic half of SWLOR's `DoorValueStore`: lock/key/transition consistency, behavior apply/clear,
  conditional lock fields, closer scripts and ordered key-item locals by convention), `IDoorBehaviorCatalog`.
- `Nwn.Authoring/Waypoints`: `WaypointBehavior` (new `PersistedId`), `IWaypointBehaviorCatalog`.
- `Nwn.Authoring/Sounds`: `SoundBehavior` (`IsLoop` becomes data), `SoundBehaviorValueStore`, `ISoundBehaviorCatalog`.
- `Nwn.Authoring/Triggers`: `TriggerBehavior`, `ITriggerBehaviorCatalog`, `TriggerFieldDefinition` (a
  behavior field that is visible only while another integer field holds a value, as `DoorFieldDefinition`
  does for doors), `TriggerKind` (generic, area transition, trap) and `TriggerKindReader` (the native
  `Type`/`TrapFlag` rule: a trap by type or flag, a transition by type, otherwise generic).
- `Nwn.Toolset.Avalonia/Fields`: `EditorFieldContext`, `FieldViewModel` and its Text, Integer, Float,
  Check, LocString, Dropdown, Script and ResourcePicker subclasses, `IScriptSlotHost`, `LookupOption`,
  `EditorGroup`, `FieldViewModelFactory`, `FieldStringId`/`FieldTexts` (`FieldEnglish.json`) and
  `Views/FieldView` (the eight templates, derived kinds first, with a terminal fallback).
- `Nwn.Toolset.Avalonia/Behaviors`: `BehaviorListItemViewModel`, `BehaviorRailView`, `BehaviorEditorHeader`
  (new `DirtyLabel`), `BehaviorEditorHost`, `BehaviorEditorStringId`/`BehaviorEditorTexts`
  (`BehaviorEditorEnglish.json`), `TransitionDestinationDescriber` (turns a `TransitionDestinationResult`
  into the status line a door or trigger destination row prints, one text per status).
- `Nwn.Toolset.Avalonia/Doors`, `Waypoints`, `Sounds`, `Triggers`: `DoorBehaviorEditorViewModel`,
  `DoorRowViewModel`, `DoorKeyItemViewModel`, `DoorBehaviorEditorHost`, `IDoorAppearancePreviewSource`,
  `DoorAppearanceGalleryEntry`, `Views/DoorBehaviorEditorView`; `WaypointBehaviorEditorViewModel`,
  `WaypointRowViewModel`, `WaypointBehaviorEditorHost`, `Views/WaypointBehaviorEditorView`;
  `SoundBehaviorEditorViewModel`, `SoundRowViewModel`, `SoundBehaviorEditorHost`,
  `Views/SoundBehaviorEditorView`; `TriggerBehaviorEditorViewModel`, `TriggerRowViewModel`,
  `TriggerBehaviorEditorHost`, `Views/TriggerBehaviorEditorView`.
- `Nwn.Toolset.Avalonia/Areas/Properties`: `AreaPropertiesPageViewModel`, `AreaPropertyFieldGroups`
  (descriptors and editors from `AreaPropertyCatalog`), `AreaPropertyFieldPolicy`,
  `AreaInstanceSectionViewModel`, `AreaInstanceSectionHost`, `AreaInstanceSectionDefinition`,
  `AreaInstanceSectionCatalog` (the eight GIT lists in SWLOR's order), `InstanceRow`,
  `PaletteBrowserViewModel`, `PaletteNodeViewModel`, `AreaInstanceEditorFactory`,
  `AreaPropertiesStringId`/`AreaPropertiesTexts` (`AreaPropertiesEnglish.json`) and
  `Views/AreaPropertiesPageView`. The page has no scroll viewer of its own; a host places it in its
  scrollable Properties tab (`AreaEditorLayout.Properties`) and keeps one scroll offset per document.

Hosts supply these. Prompts and the log are the palette's interfaces, implemented once per host.

| Interface | Members | SWLOR supplies | Xenomech supplies |
|---|---|---|---|
| `EditorFieldContext` (area document + transaction) | `Document`, run-edit delegate, `ResolveStrRef`, `OpenTlkRow`, `canOpenTlkRow`, `Texts` | ARE session through `AreaDocumentEditSession.ExecuteArea` (`RunAreEdit`), TLK resolver, custom TLK editor, `TlkService.IsEditableCustomStrRef` | `Documents.Area` through `ExecuteArea` plus its preview refresh, `AreaNativeChoiceCatalogReader.ReadStringResolver` |
| `IAreaPropertyChoiceSource` (optional) | `ChoicesFor(AreaPropertyFieldId)` | None; area numbers stay numbers, as before | Sky boxes and load screens from `AreaNativeChoiceCatalog` |
| `AreaPropertyFieldPolicy` (optional) | `Excluded`, `ReadOnly` | Default | `Excluded = {Name}` (its name box), `ReadOnly = {Name}` when TLK-owned if shown |
| `AreaInstanceSectionHost` | `Instances`, `Comments`, `RunEdit`, `Blueprints`, `Palettes`, `ResolveStrRef`, `Editors`, `WaypointTags`, `Log`, `Texts` | GIT/GIC sessions, `RunGitEdit` (`ExecuteInstances` + scene refresh) | `Documents.Instances`/`Comments`, `ExecuteInstances` + `QueuePropertyPreviewRefresh` |
| `IAreaInstanceBlueprintSource` | `ModuleIdentity`, `LoadBlueprint(type, resRef, indexed)` | `SwlorAreaInstanceBlueprintSource` over `ModuleWorkspace.LoadBlueprint`/`LoadIndexedBlueprint` | Database/native definition loader; module source root as identity |
| `IAreaInstancePaletteSource` (optional) | `TryLocate(type, out source)`, `Read(source)` | `SwlorAreaInstancePaletteSource`: `itp/*palcus.itp.json` | Its native palette ITPs (`NativePaletteCatalogLoader`) |
| `IAreaInstanceEditorFactory` (optional) | `CreateDoor`, `CreateWaypoint`, `CreateSound`, `CreateTrigger`, `CreateVariables` | `SwlorAreaInstanceEditorFactory`: SWLOR door/waypoint/sound editor subclasses, the shared trigger editor over `SwlorTriggerBehaviorCatalog`, `SwlorVarTablePolicy` | Shared `AreaInstanceEditorFactory` with its four hosts |
| `IAreaWaypointTagPolicy` (optional) | `IsSingletonTag`, `ResolveTag`, `CountPlacementsOutsideArea` | `SwlorAreaWaypointTagPolicy`: waypoint catalog + `ModuleTagIndex` | None (no singleton destinations) |
| `DoorBehaviorEditorHost` | `Catalog` (`IDoorBehaviorCatalog`: `All`, `Custom`, `BasicFields`, `Conventions`, `Classify`), `ResolveDestination` (`TransitionDestinationResolver`), `Appearances`, `AppearancePreviews` (`IDoorAppearancePreviewSource.Create(IReadOnlyList<DoorAppearanceGalleryEntry>)`: each appearance with the option id its gallery tile carries), `KeyItems` + common members | `SwlorDoorBehaviorCatalog`, tag index (a tag found on the other kind reads as wrong type), `DoorAppearanceCatalog`, `SwlorDoorAppearancePreviews` (thumbnail cache), game-code key items | A one-behavior catalog over its `AreaDoorPropertyFields` and `AreaInstancePropertyRows` door fields, `DoorAppearanceOption` mapped to `DoorAppearanceChoice`, `NativeAppearancePreviews`, `AreaTransitionTagResolver` over `AreaTransitionDestinationCatalog` |
| `TriggerBehaviorEditorHost` | `Catalog` (`ITriggerBehaviorCatalog`: `All`, `Custom`, `BasicFields`, `Classify`), `ResolveDestination` (`TransitionDestinationResolver`; no status line when null) + common | `SwlorTriggerBehaviorCatalog` over `TriggerBehaviorCatalog` (Area Transition, No-Spawn Zone, Exploration Note, Rest Zone, Quest Trigger, Trap, Custom) and the tag index | `XenomechTriggerBehaviorCatalog`: Generic, Area Transition (destination tag and a type including `None`) and Trap behaviors over native `Type`/`TrapFlag`, plus its `AreaTransitionDestinationCatalog` |
| `WaypointBehaviorEditorHost` | `Catalog` (`IWaypointBehaviorCatalog`: `All`, `BasicFields`, `PersistedBehaviorLocal`, `Classify`, `IsSingletonDestinationTag`) + common | `WaypointBehaviorCatalog` (module-derived, refreshed on transition changes) | A one-behavior catalog over map-note and appearance fields; no persisted local |
| `SoundBehaviorEditorHost` | `Catalog` (`ISoundBehaviorCatalog`), `AudioResources`, `Preview` (`ISoundListPreview`) + common | `SwlorSoundBehaviorCatalog`, audio resources, `SoundPreviewService` | A one-behavior catalog over `AreaSoundPropertyFields`, WAV resources/`AreaNativeChoiceCatalog.Sounds` |
| `BehaviorEditorHost` (common) | `ResolveChoices`, `ChoicePreviews`, `Variables` (`IVarTableSectionFactory`), `Prompts`, `Log`, `Texts` | Lookup/palette choices, `ChoicePreviewService`, `SwlorVarTableSectionFactory`, `SwlorPalettePrompts`, `SwlorPaletteLog` | `AreaNativeChoiceCatalog` choices by key, plain locals, its palette prompts and `IGameLog` adapter |
| Text catalogues | `FieldTexts`, `BehaviorEditorTexts`, `AreaPropertiesTexts` | English defaults | Built from `DesktopStrings` |

A placed trigger gets the typed trigger editor beside the area's own transform and geometry form (width and
height stay in `AreaInstanceDetailForm`; the tag moves into the editor's Basic tab as it does for a door).
The editor is behavior-shaped like the others: the rail picks what the trigger is for, a switch confirms
what it clears and writes the behavior's managed values (`Type`, `TrapFlag`, scripts) in one undo step, the
raw behavior alone exposes the locals, a `TriggerFieldDefinition` row appears only while its condition holds
(and a hidden required row is not reported missing), and a trigger with no host data keeps the generic form and its
variables. A destination row says what its tag reaches (`TransitionDestinationDescriber`): resolved and where,
not found, ambiguous, found on the other kind, part of the module unreadable, type unset, or type deliberately
None. Doors use the same statuses; `LinkedToFlags` 1 and 2 pick the door or waypoint scope, any other value
asks the host for `TypeUnset` or `TypeNone`. A trigger blueprint is outside this page: the SWLOR blueprint
trigger editor stays SWLOR's, sharing the same `TriggerBehavior` data type and catalog.

SWLOR keeps thin adapters with their existing constructors: `InstanceListSectionViewModel` derives from
`AreaInstanceSectionViewModel` and adds `RefreshWaypointCatalog`; `DoorEditorViewModel` derives from
`DoorBehaviorEditorViewModel` and adds its model preview (`IModelPreviewSource`, `PreviewView`);
`WaypointEditorViewModel` and `SoundEditorViewModel` derive from the shared editors; `DoorValueStore` and
`SoundValueStore` bind the shared stores to SWLOR's conventions. Adapters live under
`SWLOR.Toolset/Editors/AreaPropertiesHost`. `AreaEditorViewModel` builds an `AreaPropertiesPageViewModel`
and forwards `AreaPropertyGroups` and `AreaPropertiesExpanded`; `AreaEditorView` hosts
`AreaPropertiesPageView` inside its existing `PropertiesScroll`; the door, waypoint and sound documents host
the shared editor views; `App.axaml` draws every schema field through the shared `FieldView`. SWLOR's
catalogs, `DoorEditorLayout`/`WaypointEditorLayout`/`SoundEditorLayout`, `LookupOptionProvider`,
`ChoicePreviewService`, `ModelPreviewControl`, `AreSchema` (still used by its schema-wide checks) and its
transactions stay SWLOR's.

Behaviour is unchanged apart from the placed-trigger editor and the richer destination statuses (a door whose
tag only exists on the other kind now reads "this tag belongs to a waypoint, not a door" instead of "no door
carries this tag"). Invisible differences: SWLOR's door-behavior id checks become data
(`KeyRequiredRule` on Locked Door and Area Transition; the raw behavior's local sweep keys on
`AllowsVariables`, true only for Custom), `SoundBehavior.IsLoop` and `WaypointBehavior.PersistedId` are set
by the catalogs instead of computed from ids, the TLK shortcut predicate is passed explicitly, the section
titles come from `AreaInstanceSectionCatalog` rather than a private table, the grid captions are applied by
the view when the grid attaches, and a selected section re-announces `UsesGenericDetailEditor`.
`UsesDoorEditor` now also requires an editor factory; SWLOR always has one.

The Xenomech-only `AreaInstanceSectionsView` (a per-kind flat list with Add-from-palette, Duplicate and
Delete buttons driven by `AreaContentsViewModel` through `IAreaInstanceSectionActions`) is replaced by the
SWLOR-derived page. The view, `IAreaInstanceSectionActions`, the `AreaContentsViewModel` members only it used
(`InstanceSections`, the three labels and four commands) and their three `AreaContents` strings are removed;
Area Contents keeps its tree, filter, grouping and actions.

Xenomech-only behaviour not in the shared editors, kept as host extensions:

- The transition destination status: Xenomech's duplicate, wrong-type, incomplete-catalog and
  unknown-type results map onto `TransitionDestinationResult` through its `TransitionDestinationResolver`,
  so the shared door and trigger rows describe them.
- A `None` destination-type choice: SWLOR's Area Transition offers Door and Waypoint only and leaves the
  stored flag unselected when unset. Xenomech's behaviors declare their own three choices and answer
  `TypeNone` for a tagged destination whose type is None.
- Area name edits through `AreaEditingSession.SetName` with the rejected-edit status line; the page excludes
  Name for Xenomech as it does today.

Every generic door field Xenomech edits (lock, key, key tag, lock DCs, trap type/flags/DCs, transition,
faction, portrait, conversation, load screen, hit points, hardness, saves and the fourteen scripts) is
already a field of SWLOR's behaviors or Custom behavior, and the waypoint map-note/appearance and sound
playback fields likewise appear among SWLOR's behaviors, so the shared editors include them through the host catalog without changing
SWLOR's layout.

Xenomech adoption plan (`tools/Xenomech.Toolset.Desktop`):

- Delete `Areas/AreaDocumentPropertyState.cs`, `AreaDocumentPropertyGroup.cs` and
  `AreaDocumentPropertiesView.axaml[.cs]`; build an `AreaPropertiesPageViewModel` from an
  `EditorFieldContext` over `Documents.Area` (run-edit = `ExecuteArea` + `QueuePropertyPreviewRefresh`), an
  `IAreaPropertyChoiceSource` over `AreaNativeChoiceCatalog.Skyboxes`/`LoadScreens` (with
  `EnsureNumericCurrent` for unknown stored values) and `AreaPropertyFieldPolicy` excluding Name.
- Delete its desktop `Areas/AreaInstanceDetailState.cs`, `AreaInstancePropertyRows.cs`,
  `AreaDoorPropertyFields.cs`, `AreaWaypointPropertyFields.cs` and `AreaSoundPropertyFields.cs` as editor
  code; move their field lists into three catalog adapters (`IDoorBehaviorCatalog`,
  `IWaypointBehaviorCatalog`, `ISoundBehaviorCatalog`), each with one raw behavior (`AllowsVariables`) and
  `DesktopStrings` labels, and pass them through `AreaInstanceEditorFactory`.
- In `AreaEditorPanel`, replace `_instanceSections`, `_instanceForm`, `_areaProperties` and
  `_nativeProperties` (and `UpdateInstanceForm`, `SetInstanceForm`, the gallery/row/sound-list/variable
  stacking and the `IAreaInstanceSectionActions` implementation) with one `AreaPropertiesPageView` in the
  Properties tab: one `AreaInstanceSectionViewModel` per `AreaInstanceSectionCatalog` entry over
  `Documents.Instances`/`Comments`, run-edit = `ExecuteInstances` + preview refresh, its blueprint and palette
  adapters, `RowsRefreshed` driving `RefreshInstanceList`, and scene selection mapped to the section's
  `SelectedRow` as SWLOR's `ApplySelection` does. Duplicate/Delete/Copy/Paste from the scene call the
  section's `DeleteInstances`, `CopyInstanceForPlacement` and `AddCopiedInstanceAt`.
- Keep as data: `Areas/Properties/AreaNativeChoiceCatalogReader.cs` and `AreaNativeChoiceCatalog.cs` (choices
  by key for `ResolveChoices`, door appearances, sounds, TLK resolver), `AreaTransitionDestinationCatalog`
  and its resolution types (the `ResolveTag` source, plus the status-row extension above), and
  `NativeAppearancePreviews` behind `IDoorAppearancePreviewSource`.
- Remove the desktop strings covered by `AreaPropertiesTexts`/`BehaviorEditorTexts`, or build those
  catalogues from its own; load the shared `ToolsetTheme`/`ToolsetStyles` so the page's brushes and classes
  match SWLOR's.

Verification: the shared solution builds with zero warnings and errors; Formats
159/159, Authoring 157/157, Preview 153/154 (one licensed-install check inconclusive without
`XENOMECH_NWN_INSTALL_ROOT`) and Avalonia controls 185/185 pass with `XENOMECH_TEST_CONTENT_ROOT` and
`SWLOR_TEST_HAKS_ROOT` set. New coverage: field editors and text catalogues, the catalog projection with
host choices and policy, instance sections with fakes (palette add, duplicate, multi-delete with aligned GIC,
missing/unreadable palettes, typed-editor selection, singleton tags, same-module paste), door/waypoint/sound
editor behavior switching, and a headless render of the complete Properties page. SWLOR in source mode
passes its related editor tests 313/313 and its full suite in 35 class chunks 3801 passed, 6 skipped, 0
failed (3807 executed; its one `[Explicit]` corpus report is manual-only). Markup checks for the moved views
and templates move to `BehaviorEditorMarkupTests` and `FieldViewMarkupTests`; SWLOR's checks now assert that
its documents and `App.axaml` host the shared views and that the old view files do not return. No package was
built or published.

## Shared viewport display options

`Nwn.Toolset.Avalonia/Areas/AreaViewportDisplayOptions.cs` is the one observable model for the four
view-only area viewport switches (area lighting, fog, ceilings, material maps; defaults off, off, off, on).
It reads its starting values through `IAreaViewportDisplayPersistence.Load`, calls `Save` with the complete
`AreaViewportDisplaySettings` value after every change, and `ApplyTo(AreaViewportControl)` pushes the four
values onto a viewport. Persistence stays host-specific:

- SWLOR backs the interface with `ToolsetSettings` (`showAreaLighting`, `showFog`, `showCeilings`,
  `showMaterialMaps` keys unchanged); its toolbar keeps its own markup and the reserved Shadows button.
- Xenomech backs it with its workspace sidecar store (`toolset/area-viewport-display.json`), keeping the
  version check, size cap, external-change refusal and load/save problem reporting; its toolbar view binds to
  the shared model and keeps `CanEdit` and the status message in a host wrapper.

The toolbars are not shared: SWLOR's is XAML styled by its shell, Xenomech's is built in code with its own
string catalogue, and one view serving both would change SWLOR's look. The duplicated model and viewport
push logic is deleted from both apps.
## Shared blueprint model resolution

SWLOR's `BlueprintModelResolver` (same MIT first-party source, copyright 2019 Zunath, covered by
`licenses/SWLOR-MIT.txt`) is extracted into `Nwn.Authoring/Appearances` as the pure-data
`ModelReferenceResolver`. It decides which model a UTC, UTI, UTP, UTW or UTD blueprint previews from
its appearance fields and 2DA rows, and returns a `ModelReference` (`None`, `Simple`, `Segmented` or
`ItemComposite`, with `ModelPartReference` parts). It loads no model, texture or resource; the host
renderer composes the result. Xenomech's creature body, equipment and door/placeable/waypoint
lookups previously duplicated this logic; both applications now call the shared resolver.

What moved, with SWLOR's rules unchanged:

- Segmented creature naming `p{gender}{race}{phenotype}_{part}{number:D3}`, creature-or-armor part
  precedence, robe, head, and the UTI armor mannequin (`pmh0`/`pfh0`) with its cape variant.
- Visible equipment: helmet, cloak, right/left hand, shield, composite `_b_/_m_/_t_` weapons,
  ModelType 0/1 ground models, the `cloakmodel.2da` model/texture rewrite, cloak shoulder hiding, and
  retargeting a cloak from the mannequin prefix to the wearer's own body prefix.
- Door selection (specific `doortypes.2da` over generic `genericdoors.2da`, `GenericType_New` before
  legacy `GenericType`) and transition detection from `VisibleModel=0`. Door ids are read as 32-bit
  values, so a Dword sentinel such as `0xFFFFFFFF` is an unknown type rather than an overflow.
- PLT palette extraction from creature and armor color fields (`ModelPaletteResolver`,
  `ModelPaletteLayers`).

The host supplies `IModelResolutionHost`: 2DA rows (`CreatureAppearanceModelRow`, `ModelNameRow`,
`DoorModelRow`, `BaseItemModelRow`, `CloakModelRow`), table availability, item blueprint loading and
part-model existence. Optional members with defaults carry application rules: extended (above-byte)
item part numbers, shield detection, whether unset palette layers fill to row 0, a stand-in model for
an item with no model (SWLOR's loot bag), and extra creature attachments (SWLOR's wings and tails).
SWLOR keeps its tint-map overrides and `AppearanceArmor` slots, which it derives from each part's
`TintSourceItem`; robe-coverage geometry stays with its renderer.

Verification: the shared solution builds with zero warnings and errors; `ModelReferenceResolverTests`
(15 tests, fixtures for creatures, equipment, items, doors, placeables, waypoints and palettes) pass
with the existing shared suites.
## Area generator move

The generator is SWLOR's procedural area builder, taken from its `claude/swlor-generator` worktree at
`8b4f68d26` (first-party MIT source, copyright 2019 Zunath, retained in `licenses/SWLOR-MIT.txt`). Everything
generic moved; every game-specific name, ResRef and tuning value stayed with its application and reaches the
generator through host interfaces. The move kept the planners' source: for SWLOR, 3012 seeded cases (every
theme x tier x two sizes x three seeds, plus every tileset-profile x layout-profile pairing at two seeds with
default and compact/200% dressing) produce byte-identical layouts, planned dressing and populated ARE/GIT/GIC
documents before and after.

Shared types by folder:

| Folder | Types |
|---|---|
| `Nwn.Authoring/Areas/Generation/Composition` | `DungeonDetail` (theme), `DungeonTierDetail`, `DungeonCreatureEntry`, `DungeonTilesetProfile` + builder, `DungeonLayoutProfile` + builder, `DungeonComposition`, `DungeonDefinitionBuilder`, `DungeonTileLighting` |
| `.../Decoration` | `DungeonDecorationPlanner`, `DecorationPlacementSafety`, `PlannedDecoration`, `DecorationBounds`, palette entry/profile/vignette types, `DungeonTilesetPaletteInheritance` |
| `.../Frontage` | `BuildingFrontagePlanner`, `BuildingFrontageEntry`, `FacadeMountEntry`, `FrontageSupportRule` |
| `.../Atmosphere` | `DungeonAreaAtmosphere` |
| `.../Drafting` | `AreaGenerationAuthoringService`, `AreaGenerationSettings`, `LayoutKnobOverrides`, `LayoutSupportRules`, `GenerationEngine`, `AreaGenerationDraft`, `GenerationResult`, `AreaSettingsBounds`, `AreaGenerationLayoutSolver`/`AreaGenerationTileResolver` (inject the protected-cell rule into the layout solver) |
| `.../Population` | `GeneratedAreaDocumentPopulator` (tiles, lighting, atmosphere, transitions, treasure, encounters, dressing) |
| `.../Preview` | `AreaGenerationPreviewRenderer`, `AreaPreviewMode`, `AreaPreviewImage` |
| `.../Hosting` | the host interfaces below, `AreaGenerationCatalog`, `GeneratorTileset`, `GeneratedAreaRequest`, no-op/empty defaults |
| `Nwn.Toolset.Avalonia/Areas/Generation` | `AreaGeneratorViewModel`, `AreaGeneratorWindow`, `AreaGeneratorHost`, `AreaGeneratorLauncher`, `IAreaGeneratorSession`, option-label converter, choice records, background runner |
| `Nwn.Toolset.Avalonia/Localization` | `AreaGeneratorStringId`, `AreaGeneratorTexts`, `AreaGeneratorLabels`, `AreaGeneratorEnglish.json` |

Host interfaces:

| Interface | The host decides |
|---|---|
| `IAreaGenerationCatalog` | Themes, tileset profiles and layout profiles. Themes may be empty; the generator then composes a tileset and layout into geometry only. |
| `IAreaGenerationTilesetSource` | Which tilesets exist and how to load one; `GeneratorTileset.Fingerprint` travels on the draft. |
| `IGeneratedAreaBlueprintSource` | Door, placeable and creature blueprints and the creature collision radius. |
| `IGeneratedAreaPopulationPolicy` | What makes a placed instance an exit, a loot container or a safe encounter creature. |
| `IGeneratedAreaWriter` | The module transaction. It receives a `GeneratedAreaRequest` whose `Populate` callback it runs on the new documents. |
| `IAreaPreviewTileGraphics` | 2D tile pictures for Map graphics mode. |
| `IAreaGenerationLog` | Diagnostics. |
| `IAreaGeneratorSession` | Saving editors first, the module write scope, and registering and opening the created area (`AreaGeneratorLauncher` sequences them). |
| `IAreaGeneratorBackgroundTaskRunner`, `AreaGeneratorTexts`, `AreaGeneratorWindowOptions` | Threading, replacement text, window title and icon. |

`DungeonDetail` carries no default ResRefs: a host names its own exit placeable, exit door and treasure
container. SWLOR keeps `Hosting/Swlor*` adapters (catalog reflection, `TilesetCatalog`, `ModuleWorkspace`,
`NewAreaWriter`, `TextureLoader`, Serilog), its 14 themes, `StandardTilesetProfiles`, `BaseGameTilesetProfiles`,
`StandardLayoutProfiles`, and its loot-table, quest-local and treasure-script rules. The generator window has no
persisted settings in SWLOR, so no settings interface exists; the window options replace them.

Xenomech adoption (`tools/Xenomech.Toolset.Desktop/Areas/Generation`): its form state, coordinator, profile,
request, preview and window are replaced by `AreaGenerationHostFactory`, which supplies

- a catalog with no themes, five layout profiles (one per `DungeonLayoutStyle`) and one tileset profile per
  well-formed `.set` in the workspace, with `PrimaryOpenTerrain` set to the tileset's floor and no tile lighting
  so the template's lights are left alone;
- `AreaGenerationTilesetSource`, which re-reads the `.set` on every solve and fingerprints its SHA-256;
- `AreaGenerationWriter`, which refuses a draft whose tileset hash changed and calls
  `AreaCreationService.TryCreateGenerated` with the shared populator.

Without themes Xenomech areas contain tiles only. Real content needs, per theme: an exit placeable, exit door
and treasure container blueprint, tiers with creatures, a boss and a loot table id, a decoration palette, and
matching tileset-profile decoration sets; a `IGeneratedAreaBlueprintSource` over its native blueprints; and a
population policy. Xenomech's 3D viewport preview was not kept: it would change the window layout SWLOR
defines, and the 2D schematic/map preview shows the same tile result. Xenomech's former open-terrain picker is
replaced by the profile's primary open terrain.

Verification: the shared solution builds with zero warnings and errors; Formats 159/159, Authoring 181/181 (24
new generator checks over fixture catalogs: authoring service, decoration placement, population, preview,
catalog), Preview 153/154 (the same licensed-install skip) and Avalonia 196/196 (11 new: view model, window,
launcher) pass with `XENOMECH_TEST_CONTENT_ROOT` and `SWLOR_TEST_HAKS_ROOT` set.
