using ClientAPI.Configuration;
using ClientAPI.Services;
using ClientAPI.Services.Integrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
