using Craftmatrix.org.Services;
using Craftmatrix.org.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

DotEnv.Load();

var Origin = "_SaveTrackOrigin";

// Retrieve DB connection parameters from environment variables
string dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new ArgumentNullException(nameof(dbHost));
string dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? throw new ArgumentNullException(nameof(dbPort));
string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? throw new ArgumentNullException(nameof(dbUser));
string dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? throw new ArgumentNullException(nameof(dbPass));
string dbName = Environment.GetEnvironmentVariable("DB_DB") ?? throw new ArgumentNullException(nameof(dbName));

string connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};";


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<MySQLService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Craftmatrix SaveTrack API",
        Version = "v1",
        Description = "API Version 1.0"
    });
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Craftmatrix SaveTrack API",
        Version = "v2",
        Description = "API Version 2.0"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
    {
        new OpenApiSecurityScheme{
            Reference = new OpenApiReference{
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Scheme = "Bearer",
            Name = "Bearer",
            In = ParameterLocation.Header,

        },
        new string[]{}
    }});
});
builder.Services.AddControllers();

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: Origin,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:7000", "https://savetrackdev.craftmatrix.org", "https://savetrack.craftmatrix.org")
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"));

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
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

app.UseCors(Origin);

// Configure the HTTP request pipeline.
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.Run();
