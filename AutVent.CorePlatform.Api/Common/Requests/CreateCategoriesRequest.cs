using System.ComponentModel.DataAnnotations;

namespace AutVent.CorePlatform.Api.Common.Requests;

public sealed class CreateCategoriesRequest
{
    [Required]
    [MinLength(1)]
    public IList<CreateCategoryRequest> Categories { get; init; } = [];
}
