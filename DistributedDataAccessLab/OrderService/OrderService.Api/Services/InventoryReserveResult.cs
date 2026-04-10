namespace OrderService.Api.Services;

public enum InventoryReserveResult
{
    Success,
    /// <summary>No inventory row for this productId (call createOrUpdate first).</summary>
    NoInventoryRecord,
    InsufficientStock
}
