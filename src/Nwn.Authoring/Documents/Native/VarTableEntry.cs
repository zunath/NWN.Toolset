using System.Collections;
using System.Globalization;
using System.Text;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Documents.Native
{
    /// <summary>
    /// One row of a "VarTable" list: a local variable's Name (cexostring), Type (dword: 1 = int,
    /// 2 = float, 3 = string, matching NWN's local-variable type convention) and Value (typed
    /// per Type).
    /// </summary>
    public sealed class VarTableEntry
    {
        internal JsonGffStruct Struct { get; }

        internal VarTableEntry(JsonGffStruct target)
        {
            Struct = target;
        }

        public string Name => Struct.GetStringOrNull("Name") ?? string.Empty;

        public int Type => Struct.GetUIntOrNull("Type") is { } value ? (int)value : 0;

        public int? IntValue => Type == VarTable.TypeInt ? Struct.GetIntOrNull("Value") : null;

        public float? FloatValue => Type == VarTable.TypeFloat ? Struct.GetSingleOrNull("Value") : null;

        public string? StringValue => Type == VarTable.TypeString ? Struct.GetStringOrNull("Value") : null;
    }

}
