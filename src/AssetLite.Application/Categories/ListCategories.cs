using AssetLite.Application.Abstractions;
using ErrorOr;

namespace AssetLite.Application.Categories;

/// <summary>Returns all categories ordered by name.</summary>
public sealed record ListCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

/// <summary>Handles <see cref="ListCategoriesQuery"/>.</summary>
/// <param name="CategoryRepository">Category repository port.</param>
public sealed class ListCategoriesHandler(ICategoryRepository CategoryRepository)
    : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<CategoryDto>>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await CategoryRepository.ListAsync(cancellationToken);
        return categories.OrderBy(category => category.Name).Select(category => category.ToDto()).ToList();
    }
}
