using System.Text.Json;

namespace AssetLite.Api.IntegrationTests;

/// <summary>Small JSON helpers over <see cref="JsonDocument"/> for terse test assertions.</summary>
internal static class JsonExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Parses an HTTP payload into a <see cref="JsonDocument"/>.</summary>
    public static JsonDocument ParseJson(this string json) => JsonDocument.Parse(json);

    /// <summary>Resolves a property by name, failing the test when it is missing.</summary>
    public static JsonElement Property(this JsonDocument document, string name) =>
        document.RootElement.GetProperty(name);

    /// <summary>Deserializes a JSON element to a type using web defaults (camelCase).</summary>
    public static T? Deserialize<T>(this JsonElement element) => element.Deserialize<T>(SerializerOptions);

    /// <summary>Reads a GUID-valued property.</summary>
    public static Guid GetGuid(this JsonDocument document, string name) => document.Property(name).GetGuid();

    /// <summary>Reads an integer-valued property.</summary>
    public static int GetInt32(this JsonDocument document, string name) => document.Property(name).GetInt32();

    /// <summary>Reads a string-valued property.</summary>
    public static string GetString(this JsonDocument document, string name) => document.Property(name).GetString()!;

    /// <summary>Reads an array-valued property.</summary>
    public static JsonElement.ArrayEnumerator EnumerateArray(this JsonDocument document, string name) =>
        document.Property(name).EnumerateArray();
}
