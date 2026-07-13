using ClientAPI.Configuration;
using ClientAPI.OpenApi;
using ClientAPI.Services;
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

// HTTP clients
builder.Services.AddHttpClient("OriginalCoreDataService");
builder.Services.AddHttpClient("NewCoreDataService");
builder.Services.AddHttpClient("DbService");

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
            .AddDocument("v1",     "Local API")
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
