using AssetLite.Api.Dispatching;
using AssetLite.Application.Offices;
using AssetLite.Domain.Identities;
using Microsoft.AspNetCore.Mvc;

namespace AssetLite.Api.Controllers;

/// <summary>Office hierarchy endpoints.</summary>
/// <param name="dispatcher">Boundary dispatcher.</param>
[Route("api/offices")]
public sealed class OfficesController(RequestDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    /// <summary>Returns the full office tree rooted at the HQ office.</summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(OfficeTreeNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetOfficeTreeQuery, OfficeTreeNodeDto>(new(), cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Returns all offices as a flat list ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OfficeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOffices(CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<ListOfficesQuery, IReadOnlyList<OfficeDto>>(new(), cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Creates an office. Omit <c>parentOfficeId</c> to create the root (HQ) office.</summary>
    /// <param name="request">The office to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(OfficeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOffice([FromBody] CreateOfficeRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOfficeCommand(
            request.Name,
            request.Code,
            request.ParentOfficeId is { } parentId ? new OfficeId(parentId) : null);

        var result = await Dispatcher.CommandAsync<CreateOfficeCommand, OfficeDto>(command, cancellationToken);
        return From(result, office => CreatedAtAction(nameof(ListOffices), office));
    }

    /// <summary>Re-parents an office (enforces cycle, depth and root rules).</summary>
    /// <param name="id">The office to move.</param>
    /// <param name="request">The new parent office.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MoveOffice(Guid id, [FromBody] MoveOfficeRequest request, CancellationToken cancellationToken)
    {
        var command = new MoveOfficeCommand(new OfficeId(id), new OfficeId(request.NewParentOfficeId));
        var result = await Dispatcher.CommandAsync(command, cancellationToken);
        return From(result, _ => NoContent());
    }
}

/// <summary>Request body for creating an office.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Code">Short code, 3-8 uppercase alphanumeric characters.</param>
/// <param name="ParentOfficeId">Parent office id, or null to create the root (HQ) office.</param>
public sealed record CreateOfficeRequest(string Name, string Code, Guid? ParentOfficeId = null);

/// <summary>Request body for moving an office.</summary>
/// <param name="NewParentOfficeId">The id of the new parent office.</param>
public sealed record MoveOfficeRequest(Guid NewParentOfficeId);
