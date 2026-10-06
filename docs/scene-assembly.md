# Placed scene assembly

`SceneAssembler.Assemble` combines immutable prepared scenes with explicit affine
transforms supplied by the application. It does not decide species, armor slots,
attachment nodes, equipment or body proportions. Each `SceneInstance` has a unique
ASCII identity, source scene and transform from that model's coordinates into the
assembled coordinates.

Node names, parent references, bitmap names and material names are scoped by the
instance identity. The application uses `SceneAssembler.ScopedName` for matching
texture dictionary keys. This permits two instances of the same model to use
different composed colors without overwriting each other's material.

Prepared vertices are already in their source model's coordinates; assembly
applies the instance transform exactly once. Normals use its inverse transpose
and are normalized without overflowing their squared length. Reflected instances
reverse both vertex and texture face winding. Source nodes, geometry, faces and
materials remain unchanged. All inputs must be finite, invertible affine
transforms. Identities are unique ignoring case. Instance, node, vertex and face
counts are bounded before allocation grows beyond their limits.

This remains static scene preparation. Source animation-presence metadata is
retained, and `AnimationTracksApplied` remains false. An application cannot label
this API alone as movement qualification or a native-client appearance match.

Validation: the neutral Preview suite passes 40/40 with zero skips on Windows and
a fresh offline Linux SDK 10.0.401 container, including the existing native SWLOR
and Xenomech resource regressions. New cases qualify hierarchy/material isolation,
nested transforms, inverse-transpose normals, reflection and invalid transforms.
Evidence: `tests/Nwn.Preview.Tests/TestResults/scene-assembly-preview-full-windows-v2.trx`
and `artifacts/linux-scene/scene-assembly-preview-linux.trx`. Both native corpora
are read-only inputs. Initial runs with an incorrect fixture expectation or corpus
path remain retained as failed evidence.
