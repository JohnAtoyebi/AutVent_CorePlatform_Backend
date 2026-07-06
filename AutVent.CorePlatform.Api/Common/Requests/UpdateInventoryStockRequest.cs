using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class UpdateInventoryStockRequest
{
    [Range(0, long.MaxValue)]
    public long Quantity { get; init; }
}
