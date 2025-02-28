using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Craftmatrix.org.Data;
using Craftmatrix.org.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load();

var Origin = "_SaveTrackOrigin";

// Retrieve DB connection parameters from environment variables
string dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new ArgumentNullException(nameof(dbHost));
string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? throw new ArgumentNullException(nameof(dbPort));
string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? throw new ArgumentNullException(nameof(dbUser));
string dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? throw new ArgumentNullException(nameof(dbPass));
string dbName = Environment.GetEnvironmentVariable("DB_DB") ?? throw new ArgumentNullException(nameof(dbName));

string connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";

string issuer = Environment.GetEnvironmentVariable("ISSUER") ?? throw new ArgumentNullException(nameof(issuer));
string audience = Environment.GetEnvironmentVariable("AUDIENCE") ?? throw new ArgumentNullException(nameof(audience));
string secretKey = Environment.GetEnvironmentVariable("SECRETKEY") ?? throw new ArgumentNullException(nameof(secretKey));

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
var logger = loggerFactory.CreateLogger<Program>();

builder.Host.UseSerilog();

// Register MySQL service
builder.Services.AddScoped<MySQLService>();

// Register DbContext with MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

// Configure controllers and JSON options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = null;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});


// Configure API Explorer for versioning
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Configure Swagger for versioning
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Craftmatrix SaveTrack API",
        Version = "v1",
        Description = "API Version 1.0"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Craftmatrix SaveTrack API",
        Version = "v2",
        Description = "API Version 2.0"
    });

    // Enable JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'"
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
            new string[] {}
        }
    });
});


// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: Origin,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:7000", "https://savetrackdev.craftmatrix.org", "https://savetrack.craftmatrix.org")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RequireSignedTokens = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };

        // Add error logging for debugging
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    logger.LogInformation("Received Token (Length: {Length}): {Token}", token.Length, token);
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                logger.LogError("JWT Authentication Failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };

    });



builder.Services.AddAuthorization();

var app = builder.Build();

// Use CORS policy
app.UseCors(Origin);

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Craftmatrix API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Craftmatrix API v2");
        options.InjectStylesheet("/swagger-ui/custom.css");
        // options.RoutePrefix = ""; // Optional: Make Swagger UI available at root
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
