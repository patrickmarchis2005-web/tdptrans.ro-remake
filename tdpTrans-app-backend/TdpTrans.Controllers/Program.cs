using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Controllers;
using TdpTrans.Repositories;
using TdpTrans.Repositories.Implementations;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

var builder = WebApplication.CreateBuilder(args);
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
var shouldRedirectToHttps = configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase);
var configuredCorsOrigins = GetConfiguredCorsOrigins(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7092;
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(30);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => IsAllowedFrontendOrigin(origin, configuredCorsOrigins))
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    SqliteConnectionStringFactory.Configure(
        options,
        builder.Configuration.GetConnectionString("DefaultConnection") ?? builder.Configuration["DATABASE_URL"],
        builder.Environment.ContentRootPath));
builder.Services.AddMemoryCache();
builder.Services.Configure<CredentialChangeEmailOptions>(builder.Configuration.GetSection("CredentialChangeEmail"));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionName = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            ? "auth"
            : path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase)
                ? "admin"
                : "default";
        var permitLimit = partitionName switch
        {
            "auth" => 12,
            "admin" => 60,
            _ => 180,
        };

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{partitionName}:{remoteIpAddress}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
});

builder.Services.AddScoped<IMissionsRepository, MissionsRepository>();
builder.Services.AddScoped<IClientsRepository, ClientsRepository>();
builder.Services.AddScoped<ITrucksRepository, TrucksRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
builder.Services.AddScoped<IObservationRepository, ObservationRepository>();

builder.Services.AddScoped<IChatMessageStore, EntityFrameworkChatMessageStore>();
builder.Services.AddSingleton<ChatConnectionManager>();
builder.Services.AddSingleton<ChatWebSocketEndpoint>();
builder.Services.AddSingleton<ICredentialChangeCodeService, CredentialChangeCodeService>();
builder.Services.AddSingleton<ICredentialChangeCodeEmailService, CredentialChangeCodeEmailService>();

builder.Services.AddScoped<IMissionsService, MissionsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAiSuspiciousActivityDetector, AiSuspiciousActivityDetector>();
builder.Services.AddScoped<ISuspiciousActivityService, SuspiciousActivityService>();
builder.Services.AddScoped<ISecurityLabService, SecurityLabService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DatabaseBootstrapper.UpgradeAsync(dbContext);
    await DatabaseBootstrapper.EnsureSecurityPolicyDefaultsAsync(dbContext);
    await DatabaseBootstrapper.EnsureUserSecurityDefaultsAsync(dbContext);
    await EnsureReservedAdminPolicy(dbContext);
}

app.UseForwardedHeaders();

if (shouldRedirectToHttps)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
}

app.UseCors("AllowReactApp");
app.UseRateLimiter();
app.UseMiddleware<SessionAuthenticationMiddleware>();
app.UseWebSockets();
app.MapGet("/health", (ApplicationDbContext dbContext) => Results.Ok(new
{
    status = "ok",
    provider = dbContext.Database.ProviderName,
    timeUtc = DateTime.UtcNow,
}));

app.Map("/ws/chat", async context =>
{
    var endpoint = context.RequestServices.GetRequiredService<ChatWebSocketEndpoint>();
    await endpoint.HandleAsync(context);
});

app.MapControllers();

app.Run();

static async Task EnsureReservedAdminPolicy(ApplicationDbContext dbContext)
{
    var adminRole = await dbContext.Roles.SingleAsync(role => role.Name == RoleNames.Admin);
    var userRole = await dbContext.Roles.SingleAsync(role => role.Name == RoleNames.User);

    var users = await dbContext.Users
        .Include(user => user.UserRoles)
        .ToListAsync();

    foreach (var user in users)
    {
        user.Email = user.Email.Trim().ToLowerInvariant();

        var adminAssignments = user.UserRoles
            .Where(userRoleAssignment => userRoleAssignment.RoleId == adminRole.Id)
            .ToList();
        var userAssignments = user.UserRoles
            .Where(userRoleAssignment => userRoleAssignment.RoleId == userRole.Id)
            .ToList();
        var isReservedAdmin = string.Equals(user.Email, ReservedAccountEmails.PrimaryAdmin, StringComparison.OrdinalIgnoreCase);

        if (isReservedAdmin)
        {
            if (adminAssignments.Count == 0)
            {
                dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
            }

            if (userAssignments.Count > 0)
            {
                dbContext.UserRoles.RemoveRange(userAssignments);
            }

            continue;
        }

        if (adminAssignments.Count > 0)
        {
            dbContext.UserRoles.RemoveRange(adminAssignments);
        }

        if (userAssignments.Count == 0)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
        }
    }

    await dbContext.SaveChangesAsync();
}

static bool IsAllowedFrontendOrigin(string origin, IReadOnlySet<string> configuredOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    var normalizedOrigin = uri.GetLeftPart(UriPartial.Authority);
    if (configuredOrigins.Contains(normalizedOrigin))
    {
        return true;
    }

    return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && (uri.Port == 5173 || uri.Port == 4173);
}

static HashSet<string> GetConfiguredCorsOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? Array.Empty<string>();
    var environmentOrigins = (configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return configuredOrigins
        .Concat(environmentOrigins)
        .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
        .Select(origin => origin.TrimEnd('/'))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
