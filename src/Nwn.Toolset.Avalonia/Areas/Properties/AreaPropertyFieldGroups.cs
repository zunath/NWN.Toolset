using Nwn.Authoring.Areas.Properties;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Fields;
using Nwn.Toolset.Avalonia.Fields;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// Projects the shared native ARE field catalog into the Properties page's field descriptors and
/// field editors. ResRef, Tileset, Width and Height describe native identity/layout and stay read-only.
/// </summary>
public static class AreaPropertyFieldGroups
{
    /// <summary>The page's groups and fields, labelled from <paramref name="texts"/>.</summary>
    public static IReadOnlyList<FieldGroup> Describe(
        AreaPropertiesTexts texts,
        IAreaPropertyChoiceSource? choices = null,
        AreaPropertyFieldPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(texts);
        policy ??= AreaPropertyFieldPolicy.Default;

        return AreaPropertyCatalog.Groups.Select(group => new FieldGroup
        {
            Title = texts.GroupTitle(group.Id),
            Fields = group.Fields
                .Where(field => !policy.Excluded.Contains(field.Id))
                .Select(field => Describe(field, texts, choices, policy))
                .ToArray(),
        }).ToArray();
    }

    /// <summary>
    /// The field editors for the area document behind <paramref name="context"/>, grouped as the
    /// catalog declares.
    /// </summary>
    public static IReadOnlyList<EditorGroup> Create(
        EditorFieldContext context,
        AreaPropertiesTexts texts,
        IAreaPropertyChoiceSource? choices = null,
        AreaPropertyFieldPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Describe(texts, choices, policy)
            .Select(group => new EditorGroup(
                group.Title,
                group.Fields
                    .Select(descriptor => FieldViewModelFactory.Create(
                        descriptor, context, key => OptionsFor(key, choices)))
                    .ToArray()))
            .ToArray();
    }

    /// <summary>The choices of a dropdown field built by <see cref="Describe(AreaPropertiesTexts, IAreaPropertyChoiceSource?, AreaPropertyFieldPolicy?)"/>.</summary>
    public static IReadOnlyList<LookupOption> OptionsFor(string? lookupKey, IAreaPropertyChoiceSource? choices) =>
        choices != null && Enum.TryParse<AreaPropertyFieldId>(lookupKey, out var id)
            ? choices.ChoicesFor(id) ?? Array.Empty<LookupOption>()
            : Array.Empty<LookupOption>();

    private static FieldDescriptor Describe(
        AreaPropertyField field,
        AreaPropertiesTexts texts,
        IAreaPropertyChoiceSource? choices,
        AreaPropertyFieldPolicy policy)
    {
        var hasChoices = field.Kind == BehaviorFieldKind.Integer && choices?.ChoicesFor(field.Id) != null;
        return new FieldDescriptor
        {
            Label = texts.FieldLabel(field.Id),
            FieldName = field.NativeName,
            Kind = hasChoices ? EditorKind.TwoDaDropdown : EditorKindFor(field.Kind),
            LookupKey = hasChoices ? field.Id.ToString() : null,
            FieldType = field.FieldType,
            IsReadOnly = field.IsReadOnly || policy.ReadOnly.Contains(field.Id),
            Description = texts.FieldDescription(field.Id),
        };
    }

    private static EditorKind EditorKindFor(BehaviorFieldKind kind) => kind switch
    {
        BehaviorFieldKind.LocalizedText => EditorKind.LocString,
        BehaviorFieldKind.Text => EditorKind.Text,
        BehaviorFieldKind.Integer => EditorKind.Integer,
        BehaviorFieldKind.Float => EditorKind.Float,
        BehaviorFieldKind.Check => EditorKind.Check,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
