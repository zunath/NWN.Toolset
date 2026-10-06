// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Formats.NativeModels;

public sealed class MdlFace
{
    public Vector3 Normal { get; set; }

    public float Distance { get; set; }

    public int SurfaceId { get; set; }

    public ushort VertexIndex0 { get; set; }

    public ushort VertexIndex1 { get; set; }

    public ushort VertexIndex2 { get; set; }
}
