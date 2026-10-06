// SPDX-License-Identifier: MIT

using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Preview.Areas.Clipboard;

/// <summary>An independent clipboard snapshot with deep-cloned native values.</summary>
public sealed record AreaInstanceClipboardEntry(
    string ModuleRoot,
    ModuleResourceType Type,
    JsonGffStruct Instance,
    JsonGffStruct? Comment,
    InstanceMarker Preview);
