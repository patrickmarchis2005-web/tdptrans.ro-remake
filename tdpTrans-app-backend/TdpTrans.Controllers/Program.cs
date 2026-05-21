using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories;
using TdpTrans.Repositories.Implementations;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;

var builder = WebApplication.CreateBuilder(args);
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(IsAllowedFrontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        SqliteConnectionStringFactory.Create(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            builder.Environment.ContentRootPath),
        sqliteOptions => sqliteOptions.MigrationsAssembly("TdpTrans.Repositories")));

var chatDatabasePath = Path.Combine(appDataPath, "chat-store.db");

builder.Services.AddScoped<IMissionsRepository, MissionsRepository>();
builder.Services.AddScoped<IClientsRepository, ClientsRepository>();
builder.Services.AddScoped<ITrucksRepository, TrucksRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IObservationRepository, ObservationRepository>();

builder.Services.AddSingleton<IChatMessageStore>(_ => new LiteDbChatMessageStore(chatDatabasePath));
builder.Services.AddSingleton<ChatConnectionManager>();
builder.Services.AddSingleton<ChatWebSocketEndpoint>();

builder.Services.AddScoped<IMissionsService, MissionsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<ISuspiciousActivityService, SuspiciousActivityService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await EnsureReservedAdminPolicy(dbContext);
}

app.UseCors("AllowReactApp");
app.UseWebSockets();

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

static bool IsAllowedFrontendOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && (uri.Port == 5173 || uri.Port == 4173);
}
