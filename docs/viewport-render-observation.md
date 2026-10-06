# Completed viewport observations and incremental marker identity

`AreaViewportControl.LastSuccessfulRenderObservation` records the exact area scene
reference, scene version and frame number only after the existing renderer draws,
evicts stale resources and completes framebuffer composition. OpenGL vendor,
renderer and version come from that active render context. Reading the observation
performs no GPU call and does not introduce another rendering path.

A scene assignment invalidates the observation. Context initialization, teardown
and an empty-scene frame clear it. A renderer already drawing a predecessor can
publish that older completed frame, so a readiness observer must compare its
scene by reference with the current `Viewport.Scene` and require a frame newer
than its pre-operation baseline. Camera readiness and decoded thumbnails do not
establish a completed model frame. This observation identifies submitted viewport
work; it does not establish native engine or official-client compatibility.

Incremental `AreaSceneComposer.BuildInstanceMarker` callers pass the instance's
zero-based index in its own GIT resource list. A marker with index -1 represents a
standalone preview rather than an editable placement. The original four-argument
API remains available for compiled callers; the explicit-index overload preserves
selection identity without rebuilding the area's unrelated tiles and instances.

The new package graph uses Formats26 and Authoring36 unchanged, Preview37 with
its explicit-index API, and Avalonia45 with frame observations. Preview37 directly
requires Authoring36, and Avalonia45 directly requires Preview37. Versions already
published to the local qualification feed remain immutable. Host adoption and real
OpenGL checks are separate evidence gates; source regression passes alone do not
establish their completion or desktop performance budgets.

`ModelPreviewControl.LastSuccessfulRenderObservation` exposes the existing model
viewport's completed draw. Avalonia46 records the exact scene and texture-map
references, an advancing frame number and the active context's OpenGL identity
after its draw calls complete. Scene or texture assignment, context teardown and
an empty-scene frame clear the observation. A consumer checks both references
against the current surface and requires a newer frame than its baseline: an
in-flight predecessor can publish after a subsequent selection. This is submitted
model work, not a guarantee of compositor presentation or native compatibility.
The observer performs no GPU work on the reader's thread. Avalonia46 retains the
normal Authoring36 and Preview37 dependencies; Avalonia45 remains immutable.

The full shared Avalonia regression suite passes 57/57 with zero skips for this
change (`artifacts/qualification/ui46-model-observation/results/ui46-model-observation-full.trx`).
Real OpenGL model observation and packaged host adoption remain separate gates.
