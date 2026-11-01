using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Areas.Editing;

public sealed record AreaInstanceCopy(ModuleResourceType Type, JsonGffStruct Instance, JsonGffStruct? Comment);
