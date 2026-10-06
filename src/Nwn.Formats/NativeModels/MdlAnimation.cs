// SPDX-License-Identifier: MIT

namespace Nwn.Formats.NativeModels;

public sealed class MdlAnimation
{
    public string Name { get; set; } = string.Empty;

    public float Length { get; set; }

    public float TransitionTime { get; set; }

    public MdlNode? GeometryRoot { get; set; }
}
