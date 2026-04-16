using EscrowApp.Components;
using EscrowApp.Features.Behaviors;
using EscrowApp.Data;
using EscrowApp.Data.Repositories;
using EscrowApp.Models;
using EscrowApp.Models.Repositories;
using EscrowApp.Events;
using EscrowApp.Infrastructure.Auth;
using EscrowApp.Infrastructure.Middleware;
using EscrowApp.Services.Strategies;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Stripe;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// === Infrastructure ===
builder.Services.AddDbContext<EscrowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Identity (ASP.NET Core Identity with EF Core storage) ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        // Password requirements
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedEmail = false; // TODO: enable in production
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<EscrowDbContext>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// === Localization ===
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
builder.Services.AddControllers()
    .AddDataAnnotationsLocalization(opts =>
        opts.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(EscrowApp.SharedResource)));

// === API Key Options ===
builder.Services.Configure<ApiKeySettings>(opts =>
{
    opts.Clients = builder.Configuration
        .GetSection(ApiKeySettings.SectionName)
        .Get<Dictionary<string, ApiKeyConfig>>() ?? [];
});

// === Platform Fee Options ===
// PlatformOptions lives in Shared/Configuration/ — cross-cutting, accessible by all layers.
// Program.cs (composition root) is the only place that wires config to the DI container.
builder.Services.Configure<EscrowApp.Shared.Configuration.PlatformOptions>(
    builder.Configuration.GetSection(EscrowApp.Shared.Configuration.PlatformOptions.SectionName));

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
var stripeApiKey = builder.Configuration["Stripe:SecretKey"] ?? string.Empty;
StripeConfiguration.ApiKey = stripeApiKey;
builder.Services.AddSingleton<IStripeClient>(new StripeClient(stripeApiKey));
builder.Services.AddSingleton(sp =>
    new PaymentIntentService(sp.GetRequiredService<IStripeClient>()));

// === Data Layer ===
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();

// === Event Bus (§0.2 UnifiedEventBus) ===
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

// === Payment Strategies (Strategy Pattern / OCP) ===
// Adding PayPal or Ethereum: register a new IEscrowPaymentStrategy here. Zero other changes.
builder.Services.AddScoped<IEscrowPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();

// === MediatR — Vertical Slice Architecture (Phase 3) ===
// Auto-discovers all IRequestHandler<,> implementations in this assembly.
// UI calls: await Mediator.Send(new HoldFundsCommand(id, pmId));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// === Response Compression ===
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = false; // Disabled for HTTPS to prevent BREACH attacks
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/javascript", "text/css"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(opts =>
    opts.Level = System.IO.Compression.CompressionLevel.Fastest);

builder.Services.Configure<GzipCompressionProviderOptions>(opts =>
    opts.Level = System.IO.Compression.CompressionLevel.SmallestSize);

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

// === Security Headers ===
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

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

app.UseResponseCompression();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapStaticAssets();

// === API Controllers ===
app.MapControllers();

// === Culture Switch Endpoint ===
var allowedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en", "es", "en-US", "es-MX" };

app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext ctx) =>
{
    if (!allowedCultures.Contains(culture))
        return Results.BadRequest("Unsupported culture.");

    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
            HttpOnly = true
        });

    return Results.LocalRedirect(redirectUri);
});

app.MapRazorComponents<EscrowApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
