using APINexus.Configuration;
using APINexus.Scalar;
using APINexus.Services;
using APINexus.Services.ApiSourceProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Options
builder.Services.Configure<ApiSourcesOptions>(builder.Configuration.GetSection("ApiSources"));
builder.Services.Configure<UsersOptions>(builder.Configuration.GetSection("Users"));
builder.Services.Configure<ScalarProxyOptions>(builder.Configuration.GetSection("Scalar"));

// Services
builder.Services.AddScoped<IApiSourceProvider, ConfigurationApiSourceProvider>();
builder.Services.AddScoped<IUserAuthenticator, ConfigurationUserAuthenticator>();
builder.Services.AddScoped<IApiSourceSpecService, ApiSourceSpecService>();
builder.Services.AddHttpClient("ApiSourceSpecFetcher");

// Authentication / authorization
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/login.html";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            // API calls should get a plain 401 instead of a redirect to the login page.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient("Gateway")
.AddHttpMessageHandler<GatewayAuthenticationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapScalarApiReference((options, httpContext) =>
{
    var sourceProvider = httpContext.RequestServices.GetRequiredService<IApiSourceProvider>();
    var proxyUrl = httpContext.RequestServices
        .GetRequiredService<IOptionsMonitor<ScalarProxyOptions>>().CurrentValue.ProxyUrl;
    ScalarDocumentComposer.Configure(options, httpContext, sourceProvider, proxyUrl);
}).RequireAuthorization();

app.MapControllers();

app.Run();
