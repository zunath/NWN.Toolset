namespace Nwn.Authoring.Areas.Placement;

/// <summary>Counts source-backed store records that differ from their canonical expansion.</summary>
public sealed record StoreInstanceSyncStatus(
    int OutOfDateMerchantRecords,
    int OutOfDateItemRecords)
{
    public bool IsCurrent => OutOfDateMerchantRecords == 0 && OutOfDateItemRecords == 0;
}
