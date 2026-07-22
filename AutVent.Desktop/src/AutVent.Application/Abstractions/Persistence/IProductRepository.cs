using AutVent.Domain.Entities;

namespace AutVent.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> SearchAsync(string? searchTerm, CancellationToken cancellationToken);

    Task UpsertAsync(IEnumerable<Product> products, CancellationToken cancellationToken);
}
