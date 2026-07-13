using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;

namespace ClientAPI.OpenApi;

internal static class OperationMetadataExtractor
{
    internal static OperationMetadata Extract(OpenApiOperationTransformerContext context, string baseUrl)
    {
        var requestType = context.Description.ParameterDescriptions
            .FirstOrDefault(p => p.Source == BindingSource.Body)?.Type;

        var responseType = context.Description.SupportedResponseTypes
            .Where(r => r.StatusCode is >= 200 and < 300)
            .Select(r => r.Type)
            .FirstOrDefault(t => t is not null && t != typeof(void));

        return new OperationMetadata(
            HttpMethod: context.Description.HttpMethod ?? string.Empty,
            RelativePath: context.Description.RelativePath ?? string.Empty,
            BaseUrl: baseUrl,
            RequestBody: requestType is not null ? BuildTypeMetadata(requestType) : null,
            SuccessResponse: responseType is not null ? BuildTypeMetadata(responseType) : null);
    }

    private static TypeMetadata BuildTypeMetadata(Type type)
    {
        var properties = type.GetProperties()
            .Select(p => new PropertyMetadata(
                Name: p.Name,
                CSharpType: ToCSharpTypeName(p.PropertyType),
                IsEnum: p.PropertyType.IsEnum))
            .ToArray();

        var enums = type.GetProperties()
            .Select(p => p.PropertyType)
            .Where(t => t.IsEnum)
            .DistinctBy(t => t.Name)
            .Select(BuildEnumMetadata)
            .ToArray();

        return new TypeMetadata(type.Name, properties, enums);
    }

    private static EnumMetadata BuildEnumMetadata(Type enumType) =>
        new(
            Name: enumType.Name,
            Values: Enum.GetValues(enumType)
                .Cast<object>()
                .Select(v => $"{v} = {(int)v}")
                .ToArray());

    private static string ToCSharpTypeName(Type type) => type switch
    {
        _ when type == typeof(string) => "string",
        _ when type == typeof(int) => "int",
        _ when type == typeof(long) => "long",
        _ when type == typeof(bool) => "bool",
        _ when type == typeof(decimal) => "decimal",
        _ when type == typeof(double) => "double",
        _ when type == typeof(float) => "float",
        _ when type == typeof(Guid) => "Guid",
        { IsEnum: true } => type.Name,
        _ => type.Name
    };
}
