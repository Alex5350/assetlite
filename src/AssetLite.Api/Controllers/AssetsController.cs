using AssetLite.Api.Dispatching;
using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Infrastructure.Labels;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace AssetLite.Api.Controllers;

/// <summary>Asset registration, search, lifecycle and label endpoints. Assets are addressed by
/// their canonical tag (e.g. <c>AST-000001</c>), which is what end users scan and print.</summary>
/// <param name="dispatcher">Boundary dispatcher.</param>
/// <param name="labelService">Renders barcode/QR label artwork.</param>
[Route("api/assets")]
public sealed class AssetsController(RequestDispatcher dispatcher, IAssetLabelService labelService)
    : ApiControllerBase(dispatcher)
{
    /// <summary>Paged asset search. All filters are optional and combined with AND semantics.</summary>
    /// <param name="officeId">Exact office filter.</param>
    /// <param name="includeDescendants">Search the office subtree instead of a single office.</param>
    /// <param name="categoryId">Category filter.</param>
    /// <param name="status">Status filter (numeric or name, e.g. 2 or "Assigned").</param>
    /// <param name="search">Contains-match over name, serial number, tag and model.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size (1-100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AssetListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        Guid? officeId = null,
        bool includeDescendants = false,
        Guid? categoryId = null,
        AssetStatus? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchAssetsQuery(
            officeId is { } office ? new OfficeId(office) : null,
            includeDescendants,
            categoryId is { } category ? new CategoryId(category) : null,
            status,
            search,
            page,
            pageSize);

        var result = await Dispatcher.QueryAsync<SearchAssetsQuery, PagedResult<AssetListItemDto>>(query, cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Returns a single asset (including assignment history) by its canonical tag.</summary>
    /// <param name="tag">Canonical asset tag, e.g. AST-000001.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{tag}")]
    [ProducesResponseType(typeof(AssetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTag(string tag, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetAssetByTagQuery, AssetDetailDto>(new(tag), cancellationToken);
        return From(result, Ok);
    }

    /// <summary>Registers a new asset. The tag is allocated sequentially (AST-000001, ...).</summary>
    /// <param name="request">The asset to register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterAssetRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterAssetCommand(
            new CategoryId(request.CategoryId),
            new OfficeId(request.OfficeId),
            request.Name,
            request.Condition,
            request.Manufacturer,
            request.Model,
            request.SerialNumber,
            request.PurchaseDate,
            request.PurchaseCost,
            request.Currency,
            request.Notes);

        var result = await Dispatcher.CommandAsync<RegisterAssetCommand, AssetDetailDto>(command, cancellationToken);
        return From(result, asset => CreatedAtAction(nameof(GetByTag), new { tag = asset.Tag }, asset));
    }

    /// <summary>Assigns an asset to a person (or reassigns when already assigned).</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="request">Assignee details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(string tag, [FromBody] AssignAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var command = new AssignAssetCommand(result.Value, request.AssigneeName, request.AssigneeEmail);
        return await DispatchAsync(command, cancellationToken);
    }

    /// <summary>Returns an assigned asset to stock.</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Return(string tag, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new ReturnAssetCommand(result.Value), cancellationToken);
    }

    /// <summary>Sends an in-stock or assigned asset to maintenance.</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/maintenance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartMaintenance(string tag, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new StartMaintenanceCommand(result.Value), cancellationToken);
    }

    /// <summary>Returns a maintenance asset to stock.</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/maintenance/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResumeFromMaintenance(string tag, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new ResumeFromMaintenanceCommand(result.Value), cancellationToken);
    }

    /// <summary>Retires an active asset (terminal state before disposal).</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/retire")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retire(string tag, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new RetireAssetCommand(result.Value), cancellationToken);
    }

    /// <summary>Disposes a retired asset (permanent terminal state).</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/dispose")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Dispose(string tag, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new DisposeAssetCommand(result.Value), cancellationToken);
    }

    /// <summary>Transfers an asset to another office.</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="request">The destination office.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{tag}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transfer(string tag, [FromBody] TransferAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await ResolveAssetIdAsync(tag, cancellationToken);
        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        return await DispatchAsync(new TransferAssetCommand(result.Value, new OfficeId(request.TargetOfficeId)), cancellationToken);
    }

    /// <summary>Returns the printable label artwork (Code 128 barcode + QR SVGs) for a tag.</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{tag}/label")]
    [ProducesResponseType(typeof(AssetLabelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabel(string tag, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetAssetByTagQuery, AssetDetailDto>(new(tag), cancellationToken);
        return From(result, asset =>
        {
            var label = labelService.Generate(asset.Tag);
            return Ok(new AssetLabelResponse(asset.Tag, label.LabelText, label.BarcodeSvg, label.QrSvg));
        });
    }

    /// <summary>Resolves a canonical tag to its asset id (lifecycle commands are id-addressed).</summary>
    /// <param name="tag">Canonical asset tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The asset id, or lookup errors (invalid tag format, unknown tag).</returns>
    private async Task<ErrorOr<AssetId>> ResolveAssetIdAsync(string tag, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.QueryAsync<GetAssetByTagQuery, AssetDetailDto>(new(tag), cancellationToken);
        return result.Then(asset => asset.Id);
    }

    /// <summary>Dispatches a no-payload command and maps Success to 204 No Content.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 on success, otherwise the problem mapping.</returns>
    private async Task<IActionResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : Application.Abstractions.ICommand
    {
        var result = await Dispatcher.CommandAsync(command, cancellationToken);
        return From(result, _ => NoContent());
    }
}

/// <summary>Request body for registering an asset.</summary>
/// <param name="CategoryId">Existing category id.</param>
/// <param name="OfficeId">Existing office id holding the asset.</param>
/// <param name="Name">Display name.</param>
/// <param name="Condition">Physical condition (numeric or name, e.g. 1 or "New").</param>
/// <param name="Manufacturer">Optional manufacturer.</param>
/// <param name="Model">Optional model.</param>
/// <param name="SerialNumber">Optional serial number.</param>
/// <param name="PurchaseDate">Optional purchase date (not in the future).</param>
/// <param name="PurchaseCost">Optional purchase cost (non-negative).</param>
/// <param name="Currency">Optional 3-letter ISO 4217 code; defaults to USD.</param>
/// <param name="Notes">Optional free-form notes.</param>
public sealed record RegisterAssetRequest(
    Guid CategoryId,
    Guid OfficeId,
    string Name,
    AssetCondition Condition,
    string? Manufacturer = null,
    string? Model = null,
    string? SerialNumber = null,
    DateOnly? PurchaseDate = null,
    decimal? PurchaseCost = null,
    string? Currency = null,
    string? Notes = null);

/// <summary>Request body for assigning an asset.</summary>
/// <param name="AssigneeName">Assignee display name.</param>
/// <param name="AssigneeEmail">Assignee email address.</param>
public sealed record AssignAssetRequest(string AssigneeName, string AssigneeEmail);

/// <summary>Request body for transferring an asset.</summary>
/// <param name="TargetOfficeId">The destination office id.</param>
public sealed record TransferAssetRequest(Guid TargetOfficeId);

/// <summary>Printable label artwork for one asset tag.</summary>
/// <param name="Tag">Canonical asset tag (e.g. AST-000001).</param>
/// <param name="LabelText">Human-readable label text.</param>
/// <param name="BarcodeSvg">Self-contained Code 128 barcode SVG.</param>
/// <param name="QrSvg">Self-contained QR code SVG linking to the asset's public page.</param>
public sealed record AssetLabelResponse(string Tag, string LabelText, string BarcodeSvg, string QrSvg);
