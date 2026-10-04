using DataProvider.Configuration;
using DataProvider.Data;
using DataProvider.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// Data access abstractions
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IStoredProcedureExecutor, DapperStoredProcedureExecutor>();

// Services
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
