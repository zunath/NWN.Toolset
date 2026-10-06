namespace Nwn.Formats.Erf;

/// <summary>The header's BuildYear/BuildDay fields are "years since 1900" / "zero-based day of
/// year" -- real calendar fields, which would make a rebuild's output depend on wall-clock time.
/// Callers that want byte-identical output for identical input (the content builder always does)
/// pass the same fixed <see cref="ErfBuildDate"/> on every build instead of "now".</summary>
public readonly record struct ErfBuildDate(uint YearsSince1900, uint DayOfYearZeroBased)
{
    /// <summary>A fixed, arbitrary epoch (2002-01-01, NWN's original ship year) used as the content
    /// pipeline's default so two builds of the same input are byte-identical without every caller
    /// having to invent its own constant.</summary>
    public static readonly ErfBuildDate ContentEpoch = FromDate(new DateOnly(2002, 1, 1));

    public static ErfBuildDate FromDate(DateOnly date) =>
        new((uint)(date.Year - 1900), (uint)(date.DayOfYear - 1));
}
