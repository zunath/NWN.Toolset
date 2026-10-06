using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Areas.Properties;

public sealed record AreaPropertyField(
    AreaPropertyFieldId Id,
    string NativeName,
    BehaviorFieldKind Kind,
    GffFieldType FieldType,
    bool IsReadOnly = false,
    int MaxLength = 0);
