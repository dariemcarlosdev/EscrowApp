using EscrowApp.Components;
using EscrowApp.Data;
using EscrowApp.Data.Repositories;
using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Infrastructure.Auth;
using EscrowApp.Infrastructure.Middleware;
using EscrowApp.Services;
using EscrowApp.Services.Strategies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Stripe;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// === Infrastructure ===
builder.Services.AddDbContext<EscrowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// === Localization ===
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
builder.Services.AddControllers()
    .AddDataAnnotationsLocalization(opts =>
        opts.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(EscrowApp.SharedResource)));

// === API Key Authentication ===
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, null);

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("ApiAccess", policy =>
        policy.RequireAuthenticatedUser());
});

// === Swagger / OpenAPI ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NexTruzt.io Escrow API",
        Version = "v1",
        Description = "REST API for third-party escrow integration. " +
                      "Authenticate via X-Api-Key header."
    });

    opts.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = ApiKeyAuthenticationHandler.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "API Key authentication. Provide your key in the X-Api-Key header."
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// === Stripe Configuration ===
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// === Data Layer ===
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();

// === Event Bus (§0.2 UnifiedEventBus) ===
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

// === Payment Strategies (Strategy Pattern / OCP) ===
// Adding PayPal or Ethereum: register a new IEscrowPaymentStrategy here. Zero other changes.
builder.Services.AddScoped<IEscrowPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();

// === Application Services ===
// EscrowManagerService kept for backward-compat. Prefer IMediator + Feature slice Commands.
builder.Services.AddScoped<IEscrowManagerService, EscrowManagerService>();

// === MediatR — Vertical Slice Architecture (Phase 3) ===
// Auto-discovers all IRequestHandler<,> implementations in this assembly.
// UI calls: await Mediator.Send(new HoldFundsCommand(id, pmId));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var app = builder.Build();

// === API Exception Middleware (must be early — before routing) ===
app.UseMiddleware<ApiExceptionMiddleware>();

// === Localization Middleware ===
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("es") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// === Swagger (Development only) ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "NexTruzt.io Escrow API v1");
        opts.RoutePrefix = "swagger";
    });
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();

// === API Controllers ===
app.MapControllers();

// === Culture Switch Endpoint ===
app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext ctx) =>
{
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

    return Results.LocalRedirect(redirectUri);
});

app.MapRazorComponents<EscrowApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
