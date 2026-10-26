using DataProvider.Binary;
using DataProvider.Configuration;
using DataProvider.Data;
using DataProvider.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IBinarySerializer, GzipJsonLinesBinarySerializer>();
builder.Services.AddSingleton<IBinaryStorage, RedisBinaryStorage>();

// Options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
if (string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
{
    throw new InvalidOperationException("Redis:ConnectionString must be configured.");
}

// Data access abstractions
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IStoredProcedureExecutor, DapperStoredProcedureExecutor>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redisOptions.ConnectionString));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisOptions.ConnectionString;
});
builder.Services.AddOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>()
    .Configure<IConnectionMultiplexer>((options, connectionMultiplexer) =>
    {
        options.ConnectionMultiplexerFactory = () =>
            Task.FromResult(connectionMultiplexer);
    });
builder.Services.AddSingleton<IBinaryDataStore, RedisBinaryDataStore>();
builder.Services.AddSingleton<IDistributedAsyncLock, RedisDistributedAsyncLock>();

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
