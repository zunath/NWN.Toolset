namespace Nwn.Formats.Mtr;

/// <summary>A material line whose semantics the reader does not interpret.</summary>
public sealed record MtrUnrecognizedDirective(int LineNumber, string Text);
