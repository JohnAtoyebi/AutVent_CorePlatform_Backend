using AutVent.Application.Contracts;
using AutVent.Domain.Entities;
using AutVent.Shared.Results;

namespace AutVent.Application.Abstractions.Services;

public interface IPosService
{
    Task<Result<Guid>> CompleteSaleAsync(CompleteSaleCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> SearchProductsAsync(string? searchTerm, CancellationToken cancellationToken);
}
