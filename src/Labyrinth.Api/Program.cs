using System.Text;
using Labyrinth.Infrastructure;
using Labyrinth.Infrastructure.Persistence;
using Labyrinth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -------------------------------------------------------
var cfg = builder.Configuration;
var gameDataPath = Path.Combine(builder.Environment.ContentRootPath,
    cfg["Labyrinth:GameDataPath"] ?? "Data/game_data.json");
var sqlite = cfg["Labyrinth:SqliteConnectionString"] ?? "Data Source=labyrinth.db";
var corsOrigins = cfg.GetSection("Labyrinth:CorsOrigins").Get<string[]>() ?? [];

var jwt = new JwtOptions
{
    Key = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required."),
    Issuer = cfg["Jwt:Issuer"] ?? "labyrinth",
    Audience = cfg["Jwt:Audience"] ?? "labyrinth",
    ExpiryHours = int.TryParse(cfg["Jwt:ExpiryHours"], out var h) ? h : 720
};

// ---- Services ------------------------------------------------------------
builder.Services.AddLabyrinthInfrastructure(sqlite, gameDataPath, jwt);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---- Database bootstrap --------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ---- Pipeline ------------------------------------------------------------
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
