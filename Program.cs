using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PasswordHasher<ApplicationUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddScoped<IChunkService, ChunkService>();
builder.Services.AddScoped<IEmbeddingService, MockEmbeddingService>();
builder.Services.AddScoped<IRagChatService, RagChatService>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("AccessToken", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments("/api"))
                {
                    context.HandleResponse();
                    var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                    context.Response.Redirect($"/Auth/Login?returnUrl={returnUrl}");
                }

                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                if (!context.Response.HasStarted && !context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.Redirect("/Auth/Forbidden");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentTeacherAdmin", policy => policy.RequireRole(UserRoles.Student, UserRoles.Teacher, UserRoles.Admin));
    options.AddPolicy("TeacherAdmin", policy => policy.RequireRole(UserRoles.Teacher, UserRoles.Admin));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.Admin));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await EnsureAuthTablesAsync(db);
    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await auth.SeedDefaultUsersAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Documents}/{action=Index}/{id?}");

app.Run();

static async Task EnsureAuthTablesAsync(AppDbContext db)
{
    await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[ApplicationUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [ApplicationUsers] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(255) NOT NULL,
        [UserName] nvarchar(255) NOT NULL,
        [FullName] nvarchar(255) NOT NULL,
        [Role] nvarchar(50) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ApplicationUsers] PRIMARY KEY ([Id])
    );
END
""");

    await db.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ApplicationUsers_Email' AND object_id = OBJECT_ID(N'[ApplicationUsers]'))
BEGIN
    CREATE UNIQUE INDEX [IX_ApplicationUsers_Email] ON [ApplicationUsers] ([Email]);
END
""");

    await db.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ApplicationUsers_UserName' AND object_id = OBJECT_ID(N'[ApplicationUsers]'))
BEGIN
    CREATE UNIQUE INDEX [IX_ApplicationUsers_UserName] ON [ApplicationUsers] ([UserName]);
END
""");

    await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationUserId] int NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByTokenHash] nvarchar(128) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_ApplicationUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END
""");

    await db.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_ApplicationUserId' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
BEGIN
    CREATE INDEX [IX_RefreshTokens_ApplicationUserId] ON [RefreshTokens] ([ApplicationUserId]);
END
""");

    await db.Database.ExecuteSqlRawAsync("""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END
""");
}
