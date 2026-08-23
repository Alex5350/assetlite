using AssetLite.Api.Dispatching;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AssetLite.Api.Controllers;

/// <summary>
/// Base class for every AssetLite controller: provides the boundary <see cref="RequestDispatcher"/>
/// and the uniform ErrorOr → <see cref="IActionResult"/> translation (see <see cref="Problem"/>).
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiControllerBase(RequestDispatcher dispatcher) => Dispatcher = dispatcher;

    /// <summary>Gets the boundary dispatcher (validation + handler dispatch).</summary>
    protected RequestDispatcher Dispatcher { get; }

    /// <summary>
    /// Translates an ErrorOr failure into an RFC 9457 problem response:
    /// <list type="bullet">
    /// <item>validation errors → <c>400</c> ValidationProblem grouped by property name,</item>
    /// <item><see cref="ErrorType.NotFound"/> → <c>404</c>,</item>
    /// <item><see cref="ErrorType.Conflict"/> → <c>409</c>,</item>
    /// <item>anything else → <c>500</c>.</item>
    /// </list>
    /// The first error's <c>Code</c> becomes the problem <c>title</c> and an <c>errors</c> array
    /// (code + description per entry) is attached as an extension, so clients can branch on the
    /// domain error catalog (e.g. <c>Asset.CannotAssignRetired</c>).
    /// </summary>
    /// <param name="errors">The errors of a failed ErrorOr result.</param>
    /// <returns>The mapped action result.</returns>
    protected IActionResult Problem(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        var primary = errors[0];

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            // Validation error codes carry the property name (set by the RequestDispatcher).
            var modelState = new ModelStateDictionary();
            foreach (var group in errors.GroupBy(error => error.Code))
            {
                modelState.AddModelError(group.Key, string.Join(" ", group.Select(error => error.Description).Distinct()));
            }

            return ValidationProblem(detail: primary.Description, modelStateDictionary: modelState);
        }

        var statusCode = primary.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(
            detail: string.Join(" ", errors.Select(error => error.Description).Distinct()),
            statusCode: statusCode,
            title: primary.Code,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = errors.Select(error => new ErrorDetail(error.Code, error.Description)).ToArray(),
            });
    }

    /// <summary>
    /// Maps an ErrorOr result onto a successful action result or the problem mapping above.
    /// </summary>
    /// <typeparam name="T">The success payload type.</typeparam>
    /// <param name="result">The ErrorOr result.</param>
    /// <param name="onSuccess">Builds the action result for the success payload.</param>
    /// <returns>The action result.</returns>
    protected IActionResult From<T>(ErrorOr<T> result, Func<T, IActionResult> onSuccess) =>
        result.Match(onSuccess, Problem);

    /// <summary>A single entry of the <c>errors</c> problem-details extension array.</summary>
    /// <param name="Code">The domain error code (e.g. <c>Asset.CannotAssignRetired</c>).</param>
    /// <param name="Description">The human-readable error message.</param>
    public sealed record ErrorDetail(string Code, string Description);
}
