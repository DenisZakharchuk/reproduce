using ClientAPI.Configuration;
using ClientAPI.OpenApi;
using ClientAPI.Services;
using ClientAPI.Services.Authentication;
using ClientAPI.Services.Integrations;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddScoped<ICodeSampleProvider, ExampleEndpointCodeSampleProvider>();
builder.Services.AddOpenApi(options =>
    options.AddOperationTransformer<CodeSamplesOperationTransformer>());

// Options
builder.Services.Configure<CoreDataServiceOptions>(builder.Configuration.GetSection("CoreDataService"));
builder.Services.Configure<DbServiceOptions>(builder.Configuration.GetSection("DbService"));
builder.Services.Configure<KeyProviderOptions>(builder.Configuration.GetSection("KeyProvider"));

// HTTP clients
builder.Services.AddHttpClient("OriginalCoreDataService");
builder.Services.AddHttpClient("NewCoreDataService");
builder.Services.AddHttpClient("DbService");
builder.Services.AddHttpClient("KeyProvider");

// Authentication — select the credential strategy from config. Adding a new
// auth mode means a new ITokenRequestFactory + one line here; KeyProvider is
// never modified (Open/Closed). NOTE: preserves the original inverted semantics
// where UseLogin == true uses the token/apiKey flow — verify this is intended.
if (builder.Configuration.GetValue<bool>("KeyProvider:UseLogin"))
    builder.Services.AddScoped<ITokenRequestFactory, ApiKeyTokenRequestFactory>();
else
    builder.Services.AddScoped<ITokenRequestFactory, LoginTokenRequestFactory>();

builder.Services.AddScoped<IKeyProvider, ClientAPI.Services.Authentication.KeyProvider>();

// Authentication scheme applied to outgoing requests. Adding a new scheme means
// a new IRequestAuthenticator + one line here; AuthenticationHandler is never
// modified (Open/Closed). UseLogin here mirrors the credential selection above.
if (builder.Configuration.GetValue<bool>("KeyProvider:UseLogin"))
    builder.Services.AddScoped<IRequestAuthenticator, BearerTokenAuthenticator>();
else
    builder.Services.AddScoped<IRequestAuthenticator, NoOpAuthenticator>();

// The DelegatingHandlers must be registered so the named client can resolve them.
builder.Services.AddTransient<RetryOnUnauthorizedHandler>();
builder.Services.AddTransient<AuthenticationHandler>();

// Named client. Handler order = registration order (outermost first):
// RetryOnUnauthorizedHandler wraps AuthenticationHandler so a 401 triggers a
// re-authenticated retry.
builder.Services.AddHttpClient("dataApiHttpClient", client =>
    {
        client.BaseAddress = new Uri("http://data-api");
    })
    .AddHttpMessageHandler<RetryOnUnauthorizedHandler>()
    .AddHttpMessageHandler<AuthenticationHandler>();

builder.Services.AddScoped<IDataApiClient, DataApiClient>();

// Integration clients — each has its own interface; no shared abstraction
builder.Services.AddScoped<IOriginalCoreDataServiceClient, OriginalCoreDataServiceClient>();
builder.Services.AddScoped<INewCoreDataServiceClient, NewCoreDataServiceClient>();
builder.Services.AddScoped<IDbServiceClient, DbServiceClient>();

// Services
builder.Services.AddScoped<IExampleService, ExampleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .AddDocument("v1", "Local API")
            .AddDocument("galaxy", "Scalar Galaxy (external)",
                "https://registry.scalar.com/@scalar/apis/galaxy?format=json");

        // Routes test requests through a proxy to avoid browser CORS blocks
        // when calling the external API. Use your own proxy for sensitive APIs.
        options.WithProxy("https://proxy.scalar.com");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
