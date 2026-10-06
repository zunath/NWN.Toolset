#nullable enable
using System;
using System.Collections.Generic;

namespace Nwn.Authoring.Areas.Generation
{
    public sealed class LayoutSolveOptions
    {
        public Func<MacroLayout, IReadOnlyCollection<(int X, int Y)>>? ProtectedFeatureCellsProvider { get; init; }
        public Action<AreaLayoutDiagnostic>? DiagnosticSink { get; init; }
    }
}
