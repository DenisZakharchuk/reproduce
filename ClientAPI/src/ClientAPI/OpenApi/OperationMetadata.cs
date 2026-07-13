namespace ClientAPI.OpenApi;

internal record OperationMetadata(
    string HttpMethod,
    string RelativePath,
    string BaseUrl,
    TypeMetadata? RequestBody,
    TypeMetadata? SuccessResponse);

internal record TypeMetadata(
    string Name,
    IReadOnlyList<PropertyMetadata> Properties,
    IReadOnlyList<EnumMetadata> Enums);

internal record PropertyMetadata(
    string Name,
    string CSharpType,
    bool IsEnum);

internal record EnumMetadata(
    string Name,
    IReadOnlyList<string> Values);   // e.g. ["OsrA = 1", "OsrB = 2"]
