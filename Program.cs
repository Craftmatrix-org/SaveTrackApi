using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Craftmatrix.org.Data;
using Craftmatrix.org.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.Load();

var Origin = "_SaveTrackOrigin";

// Retrieve DB connection parameters from environment variables
string dbHost = Environment.GetEnvironmentVariable("DB_HOST");
string dbPort = Environment.GetEnvironmentVariable("DB_PORT");
string dbUser = Environment.GetEnvironmentVariable("DB_USER");
string dbPass = Environment.GetEnvironmentVariable("DB_PASS");
string dbName = Environment.GetEnvironmentVariable("DB_DB");

string connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";

Console.WriteLine($"Connection String: {connectionString}");

var issuer = Environment.GetEnvironmentVariable("ISSUER");
var audience = Environment.GetEnvironmentVariable("AUDIENCE");
var secretKey = Environment.GetEnvironmentVariable("SECRETKEY");

// Register JWT service
builder.Services.AddSingleton(new JwtService(issuer, audience, secretKey));

// Register MySQL service
builder.Services.AddScoped<MySQLService>();

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Register DbContext with MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: Origin,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:7000")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

// Configure controllers and JSON options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = null;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();

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
    // Define Swagger documents for each API version
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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Craftmatrix API v1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Craftmatrix API v2");
        // options.RoutePrefix = ""; // Optional: Make Swagger UI available at root
    });
}

// Use CORS policy
app.UseCors(Origin);

// Middleware setup
app.UseHttpsRedirection();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
