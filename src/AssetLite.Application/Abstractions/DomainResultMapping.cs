using AssetLite.Domain.Common;
using ErrorOr;

namespace AssetLite.Application.Abstractions;

/// <summary>
/// Bridges the pure Domain result type to the ErrorOr result type used throughout the
/// Application layer. ErrorOr is referenced only here (and by feature handlers) — never by the
/// Domain assembly.
/// </summary>
/// <remarks>
/// Mapping convention: domain codes ending in <c>"NotFound"</c> map to <c>Error.NotFound</c>
/// (HTTP 404); every other domain rule violation is a business conflict and maps to
/// <c>Error.Conflict</c> (HTTP 409). The <c>Code</c> (e.g. <c>"Asset.CannotAssignRetired"</c>)
/// is preserved so the API layer can map errors to problem details without knowing the catalog.
/// Input-shape validation (HTTP 400) is handled separately by FluentValidation at the boundary.
/// </remarks>
public static class DomainResultMapping
{
    extension(DomainError error)
    {
        /// <summary>Maps a domain error to an ErrorOr error, preserving code and message.</summary>
        public Error ToError() => error.Code.EndsWith("NotFound", StringComparison.Ordinal)
            ? Error.NotFound(error.Code, error.Message)
            : Error.Conflict(error.Code, error.Message);
    }

    extension<T>(DomainResult<T> result)
    {
        /// <summary>Maps a domain result to ErrorOr (value on success, error on failure).</summary>
        public ErrorOr<T> ToErrorOr() => result.IsSuccess ? result.GetValueOrThrow() : result.Error!.ToError();

        /// <summary>Extracts the error of a failed domain result.</summary>
        public Error ToError() => result.Error!.ToError();
    }

    extension(DomainResult result)
    {
        /// <summary>Maps a non-generic domain result to <see cref="ErrorOr{Success}"/> (ErrorOr's unit).</summary>
        public ErrorOr<Success> ToErrorOr() => result.IsSuccess ? new Success() : result.Error!.ToError();

        /// <summary>Extracts the error of a failed non-generic domain result.</summary>
        public Error ToError() => result.Error!.ToError();
    }
}
