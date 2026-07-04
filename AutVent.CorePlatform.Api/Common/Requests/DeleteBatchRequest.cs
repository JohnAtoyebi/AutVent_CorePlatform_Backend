using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class DeleteBatchRequest
{
    [Required]
    [MinLength(1)]
    public IList<long> Ids { get; init; } = [];
}
