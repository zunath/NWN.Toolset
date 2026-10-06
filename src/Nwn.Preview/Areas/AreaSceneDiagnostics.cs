// SPDX-License-Identifier: MIT

namespace Nwn.Preview.Areas
{
    /// <summary>Diagnostics collected while assembling one <see cref="AreaScene"/>.</summary>
    public sealed class AreaSceneDiagnostics
    {
        private readonly List<string> _missingModels = new();

        /// <summary>
        /// Human-readable notes for every tile placement that fell back to a placeholder: bad
        /// Tile_ID, a blank Model in the tileset, an unresolvable tileset, or a model resource that
        /// could not be found/parsed.
        /// </summary>
        public IReadOnlyList<string> MissingModels => _missingModels;

        public void AddMissingModel(string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            _missingModels.Add(message);
        }
    }
}

