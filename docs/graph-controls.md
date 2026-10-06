# Directed graph controls

`Nwn.Toolset.Avalonia.Graph` provides a neutral directed diagram. Applications
map their conversation nodes, mission stages and choices to typed node/edge IDs
and display labels. It has no persistence, gameplay vocabulary or server reference.
Selecting a node raises `NodeSelected`; the application retains responsibility
for its inspector, edits, undo and database revision checks.

`GraphDocument` copies and validates its inputs. Identities are unique, labels
are bounded, and diagrams are limited to 10,000 nodes and 20,000 edges. Every
edge has a visible source. An unfinished target may remain unresolved: the
canvas draws its outgoing stub in a distinct color, with the caller's label.
It does not fabricate a node or discard that draft link.

The deterministic layout includes disconnected components and cycles. Return
and self-links route above the nodes. `GraphViewportState` keeps layout and
selection independently of the renderer. Replacing a document retains positions
for surviving identities and clears absent selections. Positions are view state,
not an implied authoring save. The canvas supports left-click selection/node
dragging, background/right/middle-button pan, pointer-anchored wheel zoom and Fit.
Fit includes return-link lanes and unresolved targets. Zoom and coordinates are
bounded; clipping avoids drawing offscreen node labels.

Windows x64 source tests pass 10/10, zero skipped, including the existing camera
and mesh-budget cases. The real headless pointer fixture checks selection,
dragging, zoom anchoring and pan, and retains a Skia-rendered frame. Evidence:
Xenomech's `artifacts/tests/toolset-authoring/shared-graph-render-qualified.trx`.
The inspected 1000×650 frame has SHA-256
`0bb7f55bd781d56eb68ecfc909e157159b9c162a4f15ef70e1f50b0493330a17`.
This is software UI evidence, separate from OpenGL/native-client qualification.

The initial Linux full UI run aborted at its sixty-second inactivity deadline
in `GraphCanvasTests`; it is not a pass. Its logs and dump are retained under
`artifacts/linux-graph-final-20261001/logs`. The separate portable Linux run
passes all nine non-rendering cases with zero skipped, using
`--filter 'TestCategory!=DesktopRender'`; it builds the actual control too.
Evidence is under `artifacts/linux-graph-portable-final-20261001/logs`.
Both runs use the fixed SDK image already recorded in
[material preview qualification](material-preview.md#qualification), network
disabled, read-only source/cache and isolated writable output. Windows x64 is
the qualified desktop target; Linux software graph rendering remains unqualified.

The component still requires application integration and packaged-consumer
proof. It does not establish a complete conversation or mission editing workflow.
