using Scriban;
using Scriban.Runtime;

namespace ClientAPI.OpenApi;

internal sealed class ExampleEndpointCodeSampleProvider : ICodeSampleProvider
{
    // Templates — variables injected per operation via Scriban ScriptObject:
    //   {{ base_url }}           server base URL
    //   {{ path }}               relative path (no leading slash)
    //   {{ enums_block }}        "public enum X { ... }\n" per enum, or ""
    //   {{ enums_inline }}       flat comment: "EnumName: V = 0, ..."
    //   {{ request.name }}       request CLR type name
    //   {{ request.params }}     "TypeA PropA, TypeB PropB, ..."
    //   {{ request.ctor_args }}  "PropA: default, PropB: default, ..."
    //   {{ response.name }}      response CLR type name
    //   {{ response.params }}    "TypeA PropA, TypeB PropB, ..."
    //   {{ response.py_hints }}  '"propA": type, ...'
    //   {{ request.py_payload }} '"propA": value, ...'

    private static readonly Template CSharpTemplate = Template.Parse("""
        {{ enums_block }}public record {{ request.name }}({{ request.params }});
        public record {{ response.name }}({{ response.params }});

        using var client = new HttpClient { BaseAddress = new Uri("{{ base_url }}") };
        var dto = new {{ request.name }}({{ request.ctor_args }});
        var response = await client.PostAsJsonAsync("/{{ path }}", dto);
        var result = await response.Content.ReadFromJsonAsync<{{ response.name }}>();
        """);

    private static readonly Template PythonTemplate = Template.Parse("""
        import requests

        # {{ enums_inline }}
        # Response {{ response.name }}: { {{ response.py_hints }} }

        payload = { {{ request.py_payload }} }
        response = requests.post("{{ base_url }}/{{ path }}", json=payload)
        result = response.json()
        """);

    public IReadOnlyList<CodeSample> GetSamples(OperationMetadata metadata)
    {
        if (!metadata.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            metadata.RelativePath != "api/example" ||
            metadata.RequestBody is null ||
            metadata.SuccessResponse is null)
            return [];

        var ctx = BuildScriptContext(metadata);
        return
        [
            new("csharp", "C# / HttpClient",  CSharpTemplate.Render(ctx)),
            new("python", "Python / requests", PythonTemplate.Render(ctx))
        ];
    }

    private static TemplateContext BuildScriptContext(OperationMetadata metadata)
    {
        var req  = metadata.RequestBody!;
        var resp = metadata.SuccessResponse!;

        var allEnums = req.Enums.Concat(resp.Enums).DistinctBy(e => e.Name).ToArray();

        var obj = new ScriptObject();
        obj["base_url"]     = metadata.BaseUrl;
        obj["path"]         = metadata.RelativePath.TrimStart('/');
        obj["enums_block"]  = BuildEnumsBlock(allEnums);
        obj["enums_inline"] = string.Join(" | ", allEnums.Select(e =>
                                  $"{e.Name}: {string.Join(", ", e.Values)}"));
        obj["request"]      = BuildTypeObject(req);
        obj["response"]     = BuildTypeObject(resp);

        var ctx = new TemplateContext { StrictVariables = false };
        ctx.PushGlobal(obj);
        return ctx;
    }

    private static ScriptObject BuildTypeObject(TypeMetadata type)
    {
        var o = new ScriptObject();
        o["name"]       = type.Name;
        o["params"]     = string.Join(", ", type.Properties.Select(p => $"{p.CSharpType} {p.Name}"));
        o["ctor_args"]  = string.Join(", ", type.Properties.Select(p => $"{p.Name}: default"));
        o["py_hints"]   = string.Join(", ", type.Properties.Select(p =>
                              $"\"{ToCamelCase(p.Name)}\": {ToPythonTypeName(p)}"));
        o["py_payload"] = string.Join(", ", type.Properties.Select(p =>
                              $"\"{ToCamelCase(p.Name)}\": {ToPythonDefault(p)}"));
        return o;
    }

    private static string BuildEnumsBlock(IEnumerable<EnumMetadata> enums)
    {
        var lines = enums
            .Select(e => $"public enum {e.Name} {{ {string.Join(", ", e.Values)} }}\n")
            .ToArray();
        return lines.Length > 0 ? string.Concat(lines) : string.Empty;
    }

    private static string ToCamelCase(string name) =>
        char.ToLowerInvariant(name[0]) + name[1..];

    private static string ToPythonTypeName(PropertyMetadata p) => p.CSharpType switch
    {
        "string"  => "str",
        "int"     => "int",
        "long"    => "int",
        "bool"    => "bool",
        "decimal" => "float",
        "double"  => "float",
        "float"   => "float",
        "Guid"    => "str",
        _         => "int"
    };

    private static string ToPythonDefault(PropertyMetadata p) => p.CSharpType switch
    {
        "string" => "\"\"",
        "bool"   => "False",
        "Guid"   => "\"00000000-0000-0000-0000-000000000000\"",
        _        => "0"
    };
}
