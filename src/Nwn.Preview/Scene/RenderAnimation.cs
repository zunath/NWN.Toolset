// SPDX-License-Identifier: MIT

namespace Nwn.Preview.Scene
{
    /// <summary>A named preview animation exposed to an editor picker and the viewport.</summary>
    public sealed class RenderAnimation
    {
        public string Name { get; init; } = string.Empty;
        public float Length { get; init; }
        public bool ShowsEmitters { get; init; }
        public bool IsPlayable { get; init; }
    }
}

