// -----------------------------------------------------------------------------
// Program.cs — NexTruzt.io EscrowApp Composition Root
// -----------------------------------------------------------------------------
// This file is the single composition root for the application. It is organized
// in two phases, separated by `var app = builder.Build();`:
//
//   PHASE 1 — Service Registration (DI container)
//     Infrastructure → Identity → Cookies → Blazor → Localization →
//     API Auth (API Key) → Authorization Policies → Swagger → Stripe →
//     Data Repositories → Event Bus → Payment Strategies (Strategy Pattern) →
//     MediatR + Validation Pipeline → Response Compression
//
//   PHASE 2 — HTTP Middleware Pipeline (order is significant)
//     ApiExceptionMiddleware → Localization → HSTS (prod) → Security Headers →
//     Swagger (dev) → Response Compression → Status Code Pages →
//     HTTPS Redirection → Authentication → Authorization → Antiforgery →
//     Static Assets → Endpoints (Controllers, Webhooks, Culture, Logout, Razor)
//
// Architectural conventions enforced here:
//   • Clean Architecture — outer layers depend on inner contracts; this file is
//     the only place that wires concrete implementations to abstractions.
//   • Strategy Pattern — payment providers implement IEscrowPaymentStrategy;
//     adding a new provider (PayPal, Ethereum) requires only a new registration.
//   • CQRS via MediatR — handlers and behaviors are auto-discovered from this
//     assembly; UI/API never call services or repositories directly.
//   • Fintech guardrails — API key auth on REST endpoints, antiforgery on Blazor,
//     manual-capture Stripe payments, idempotency keys on payment ops.
// -----------------------------------------------------------------------------

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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.ResponseCompression;
using Stripe;
using System.Globalization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// PHASE 1 — SERVICE REGISTRATION
// =============================================================================

// === Infrastructure ===
// PostgreSQL via Npgsql. Connection string lives in appsettings.json /
// environment variables — never hardcoded. EscrowDbContext also backs ASP.NET
// Identity (see AddEntityFrameworkStores below), so a single DbContext owns
// both domain tables and AspNet* identity tables.
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

// === Cookie Configuration ===
// Explicitly mark Identity and Antiforgery cookies as Secure + SameSite=Strict
// to prevent cross-scheme cookie warnings caused by VS BrowserLink injecting
// HTTP requests from an HTTPS page.
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.Cookie.HttpOnly = true;
    opts.LoginPath = "/auth/login";
    opts.AccessDeniedPath = "/auth/access-denied";
});

builder.Services.AddAntiforgery(opts =>
{
    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opts.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// === Blazor Server Authentication ===
// RevalidatingIdentityAuthenticationStateProvider extends RevalidatingServerAuthenticationStateProvider
// — the correct Blazor Server base class. It manages its own background revalidation timer,
// creates properly scoped DI contexts for each tick, and validates the user's Identity security stamp.
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider>();


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
// Custom authentication handler that validates API keys from the X-Api-Key header.
// Authentication schemes must be registered before Authorization policies. Policies reference schemes
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, null);

// === Authorization Policies ===
// Define an "ApiAccess" policy that requires authenticated users (with any valid API key).
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("ApiAccess", policy =>
    {
        // Reference the API key authentication scheme. This ensures that the policy requires API key authentication.
        policy.AuthenticationSchemes.Add(ApiKeyAuthenticationHandler.SchemeName);
        policy.RequireAuthenticatedUser();
    });
});

// === Swagger / OpenAPI ===
// API documentation with Swagger/OpenAPI. Available at /swagger in Development only.
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
// Stripe .NET SDK configuration. Stripe API key lives in configuration (appsettings.json / environment variables) — never hardcoded.
var stripeApiKey = builder.Configuration["Stripe:SecretKey"] ?? string.Empty;
StripeConfiguration.ApiKey = stripeApiKey;
builder.Services.AddSingleton<IStripeClient>(new StripeClient(stripeApiKey));
builder.Services.AddSingleton(sp =>
    new PaymentIntentService(sp.GetRequiredService<IStripeClient>()));

// === Stripe Webhook Configuration ===
// StripeWebhookOptions holds the webhook signing secret from configuration, used by StripeSignatureVerifier.
builder.Services.Configure<EscrowApp.Infrastructure.Options.StripeWebhookOptions>(
    builder.Configuration.GetSection("Stripe:Webhook"));
builder.Services.AddScoped<EscrowApp.Infrastructure.Webhooks.Stripe.StripeSignatureVerifier>();

// === Data Layer ===
// Repository pattern for data access. IEscrowTransactionRepository abstracts away EF Core and allows
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();

// === Event Bus (§0.2 UnifiedEventBus) ===
// In-memory event bus for decoupled communication between features. Suitable for single-instance apps; replace with a distributed bus (e.g., RabbitMQ) for multi-instance.
builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

// === Payment Strategies (Strategy Pattern / OCP) ===
// Adding PayPal or Ethereum: register a new IEscrowPaymentStrategy here. Zero other changes.
builder.Services.AddScoped<IEscrowPaymentStrategy, StripePaymentStrategy>();
builder.Services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();

// === MediatR — Vertical Slice Architecture (Phase 3) ===
// Auto-discovers all IRequestHandler<,> implementations in this assembly.
// UI calls: await Mediator.Send(new HoldFundsCommand(id, pmId));
//
// Pipeline behaviors run in registration order and wrap every handler:
//   1. LoggingBehavior      — structured request/response logging (no PII)
//   2. PerformanceBehavior  — emits warning when a handler exceeds SLA budget
//   3. ValidationBehavior   — runs FluentValidation; throws ValidationException
//                              on failure (translated by ApiExceptionMiddleware)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Theme service: notify server components when JS theme changes (light/dark).
// Singleton because theme state is a process-wide pub/sub channel, not per-user.
builder.Services.AddSingleton<EscrowApp.Services.ThemeService>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// === Response Compression ===
// Compresses responses using Brotli and Gzip. Disabled for HTTPS to prevent BREACH attacks (sensitive data in compressed responses can be exploited).
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

// === Health Checks ===
// Liveness endpoint for container orchestrators (Render health check, Azure
// Container Apps probe). Mapped to /health in Phase 2.
builder.Services.AddHealthChecks();

var app = builder.Build();

// =============================================================================
// PHASE 2 — HTTP MIDDLEWARE PIPELINE
// Order is significant: each middleware wraps the rest. Authentication MUST
// come before Authorization; Antiforgery MUST come after Authentication;
// Exception handling MUST be registered early so it can catch downstream throws.
// =============================================================================

// === Forwarded Headers (must be first — before HTTPS redirection/auth) ===
// Render and Azure Container Apps terminate TLS at a reverse proxy and forward
// plain HTTP to the container. Without this, the app sees scheme=http, breaking
// UseHttpsRedirection (redirect loop) and Secure cookies (login fails).
// Trust the proxy's X-Forwarded-Proto so the original https scheme is honored.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// === API Exception Middleware (must be early — before routing) ===
// Translates uncaught domain/validation exceptions into RFC 7807 ProblemDetails
// JSON for /api/* routes. Registered first so it wraps every later middleware.
app.UseMiddleware<ApiExceptionMiddleware>();

// === Localization Middleware ===
// Reads culture from .AspNetCore.Culture cookie (set by /culture/set endpoint) and applies it to the request pipeline.
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
// Sets common security headers to mitigate XSS, clickjacking, and other web vulnerabilities.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

    // In development allow hot-reload, Blazor WebSocket, and BrowserLink (VS injects
    // BrowserLink via its debugger pipeline — no config can prevent it, so we allow it).
    var connectSrc = app.Environment.IsDevelopment()
        ? "connect-src 'self' ws://localhost:* wss://localhost:* http://localhost:*;"
        : "connect-src 'self' wss:;";

    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        connectSrc);
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

// === Health Check Endpoint ===
// Anonymous liveness probe for Render / Azure Container Apps.
app.MapHealthChecks("/health").AllowAnonymous();

app.UseAntiforgery();
app.MapStaticAssets();

// === API Controllers ===
app.MapControllers();

// === Stripe Webhook Endpoint ===
// POST is the real webhook receiver (Stripe signs the body — see StripeSignatureVerifier).
// GET is a Development-only diagnostic that returns webhook configuration status;
// it is NOT used by Stripe CLI or production deliveries (those always POST).
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/webhooks/stripe", EscrowApp.Infrastructure.Webhooks.Stripe.StripeWebhookEndpoint.HandleStatus)
        .Produces<EscrowApp.Infrastructure.Webhooks.Stripe.StripeWebhookStatusResponse>(StatusCodes.Status200OK)
        .WithName("StripeWebhookStatus")
        .AllowAnonymous();
}

app.MapPost("/api/webhooks/stripe", EscrowApp.Infrastructure.Webhooks.Stripe.StripeWebhookEndpoint.HandleAsync)
   .Produces(StatusCodes.Status204NoContent)
   .Produces(StatusCodes.Status400BadRequest)
   .Produces(StatusCodes.Status401Unauthorized)
   .WithName("StripeWebhook")
   .DisableAntiforgery(); // Webhook doesn't have CSRF token

// === Culture Switch Endpoint ===
// Sets the .AspNetCore.Culture cookie and redirects back to the originating page.
// The allowlist prevents arbitrary culture injection (defense against malformed
// CultureInfo names). LocalRedirect prevents open-redirect to external hosts.
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
            SameSite = SameSiteMode.Strict,
            Secure = true,
            HttpOnly = true
        });

    return Results.LocalRedirect(redirectUri);
});

// === Logout Endpoint ===
// SignOutAsync() must run over a real HTTP response to delete the auth cookie.
// Calling it inside a Blazor Server circuit (SignalR) has no effect because
// there is no writable HTTP response — the cookie header is never sent.
app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
}).RequireAuthorization().DisableAntiforgery();

app.MapRazorComponents<EscrowApp.Components.App>()
    .AddInteractiveServerRenderMode();

// === Database Migration ===
// Applies pending EF Core migrations on startup so a freshly provisioned
// database (e.g. Neon on first Render deploy) has its schema before the role
// seeding below queries AspNetRoles. Idempotent — no-op when already current.
using (var migrateScope = app.Services.CreateScope())
{
    var db = migrateScope.ServiceProvider.GetRequiredService<EscrowDbContext>();
    await db.Database.MigrateAsync();
}

// === Role Seeding ===
// Ensures AppRoles.Client and AppRoles.Consultant exist in AspNetRoles before
// any user registration. Idempotent — safe to run on every startup.
using (var seedScope = app.Services.CreateScope())
{
    var roleManager = seedScope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<int>>>();

    foreach (var role in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<int>(role));
    }
}

app.Run();
