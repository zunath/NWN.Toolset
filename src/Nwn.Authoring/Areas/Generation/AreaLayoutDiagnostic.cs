#nullable enable
namespace Nwn.Authoring.Areas.Generation
{
    public sealed record AreaLayoutDiagnostic(
        AreaLayoutDiagnosticCode Code,
        string TilesetResref,
        string? Detail = null,
        int? RoomId = null,
        int? CandidateCount = null,
        int? SecondaryCandidateCount = null);
}

