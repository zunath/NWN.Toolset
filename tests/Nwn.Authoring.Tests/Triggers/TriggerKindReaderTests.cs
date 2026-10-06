using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Triggers;

namespace Nwn.Authoring.Tests.Triggers;

[TestClass]
public sealed class TriggerKindReaderTests
{
    [TestMethod]
    public void TheKindFollowsTheTypeFieldAndTheTrapFlagWinsOverIt()
    {
        Assert.AreEqual(TriggerKind.Generic, Read(string.Empty));
        Assert.AreEqual(TriggerKind.Generic, Read("\"Type\":{\"type\":\"int\",\"value\":0}"));
        Assert.AreEqual(TriggerKind.AreaTransition, Read("\"Type\":{\"type\":\"int\",\"value\":1}"));
        Assert.AreEqual(TriggerKind.Trap, Read("\"Type\":{\"type\":\"int\",\"value\":2}"));
        Assert.AreEqual(TriggerKind.Trap, Read("\"TrapFlag\":{\"type\":\"byte\",\"value\":1}"));
        Assert.AreEqual(
            TriggerKind.Trap,
            Read("\"Type\":{\"type\":\"int\",\"value\":1},\"TrapFlag\":{\"type\":\"byte\",\"value\":1}"));
        Assert.AreEqual(TriggerKind.Generic, Read("\"Type\":{\"type\":\"int\",\"value\":7}"));
    }

    private static TriggerKind Read(string fields)
    {
        var separator = fields.Length == 0 ? string.Empty : ",";
        var json = "{\"__data_type\":\"UTT \"" + separator + fields + "}";
        return TriggerKindReader.Read(JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json)).Root);
    }
}
