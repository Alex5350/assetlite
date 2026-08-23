using System.Text.Json;
using System.Text.Json.Serialization;
using AssetLite.Domain.Identities;

namespace AssetLite.Api.Serialization;

/// <summary>
/// Serializes the domain's strongly typed ids (<see cref="OfficeId"/>, <see cref="CategoryId"/>,
/// <see cref="AssetId"/>, <see cref="AssignmentId"/>) as raw GUID strings and parses them back,
/// keeping API contracts primitive-typed (<c>"id": "0199..."</c> instead of
/// <c>"id": { "value": "0199..." }</c>). Nullable ids (e.g. <c>OfficeDto.ParentOfficeId</c>) are
/// handled by System.Text.Json's built-in nullable wrapping.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly Dictionary<Type, Func<JsonConverter>> Converters = new()
    {
        [typeof(OfficeId)] = () => new GuidIdConverter<OfficeId>(id => id.Value, value => new OfficeId(value)),
        [typeof(CategoryId)] = () => new GuidIdConverter<CategoryId>(id => id.Value, value => new CategoryId(value)),
        [typeof(AssetId)] = () => new GuidIdConverter<AssetId>(id => id.Value, value => new AssetId(value)),
        [typeof(AssignmentId)] = () => new GuidIdConverter<AssignmentId>(id => id.Value, value => new AssignmentId(value)),
    };

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => Converters.ContainsKey(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Converters[typeToConvert]();

    private sealed class GuidIdConverter<TId>(Func<TId, Guid> getValue, Func<Guid, TId> create) : JsonConverter<TId>
        where TId : struct
    {
        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a GUID string for {typeToConvert.Name}.");
            }

            Guid guid;
            try
            {
                guid = reader.GetGuid();
            }
            catch (FormatException)
            {
                throw new JsonException($"Value is not a valid GUID for {typeToConvert.Name}.");
            }

            return create(guid);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(getValue(value));
    }
}
