using AmlOps.Backend.Application;
using AmlOps.Backend.Infrastructure;
using AmlOps.Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var bindUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5064";
builder.WebHost.UseUrls(bindUrl);

builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AML Ops REST API",
        Version = "v1",
        Description = "AML MVP backend APIs for cases, workflow, import, dashboard, admin, and evidence export."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token as: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AmlOpsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("AmlOpsDb"),
        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "aml")));

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "amlops-local";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "amlops-frontend";
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "CHANGE_ME_TO_A_LONG_RANDOM_DEV_KEY_123456789";
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = jwtKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplicationServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AmlOpsDbContext>();
    await db.Database.MigrateAsync();
    await DemoSeed.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AML Ops REST API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("Frontend");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapAmlEndpoints();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
