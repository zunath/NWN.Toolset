namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>The outcome of launching the Area Generator and, when an area was created, its ResRef.</summary>
public sealed record AreaGeneratorLaunchResult(AreaGeneratorOutcome Outcome, string CreatedResRef = "");
