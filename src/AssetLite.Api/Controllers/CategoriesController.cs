using AssetLite.Api.Dispatching;
using AssetLite.Application.Categories;
using AssetLite.Domain.Identities;
using Microsoft.AspNetCore.Mvc;

namespace AssetLite.Api.Controllers;

/// <summary>Asset category endpoints.</summary>
/// <param name="dispatcher">Boundary dispatcher.</param>
[Route("api/categories")]
public sealed class CategoriesController(RequestDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    /// <summary>Returns all categories ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<ListCategoriesQuery, IReadOnlyList<CategoryDto>>(new(), cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Creates a category (unique name).</summary>
    /// <param name="request">The category to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(request.Name, request.Description, request.ExpectedLifespanMonths);
        var result = await Dispatcher.CommandAsync<CreateCategoryCommand, CategoryDto>(command, cancellationToken);
        return From(result, category => StatusCode(StatusCodes.Status201Created, category));
    }

    /// <summary>Updates a category's editable fields.</summary>
    /// <param name="id">The category to update.</param>
    /// <param name="request">The new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(new CategoryId(id), request.Name, request.Description, request.ExpectedLifespanMonths);
        var result = await Dispatcher.CommandAsync<UpdateCategoryCommand, CategoryDto>(command, cancellationToken);
        return From(result, Ok);
    }
}

/// <summary>Request body for creating or updating a category.</summary>
/// <param name="Name">Display name (unique).</param>
/// <param name="Description">Optional description.</param>
/// <param name="ExpectedLifespanMonths">Expected lifespan in months (positive).</param>
public sealed record CategoryRequest(string Name, string? Description, int ExpectedLifespanMonths);
