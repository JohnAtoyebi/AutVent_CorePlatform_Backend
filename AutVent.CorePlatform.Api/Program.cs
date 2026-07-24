using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AutVent.CorePlatform.Api;
using AutVent.CorePlatform.Api.Common.Responses;
using AutVent.CorePlatform.Api.Common.Security;
using AutVent.CorePlatform.Api.Infrastructure;
using AutVent.CorePlatform.Api.Services;
using AutVent.CorePlatform.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostPolicy", builder =>
    {
        builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err =>
                new ApiError(
                    Code: "ValidationError",
                    Message: err.ErrorMessage,
                    Field: e.Key)))
            .ToArray();

        var response = ApiResponse<object>.ValidationFailed(
            "One or more validation errors occurred",
            errors,
            traceId: context.HttpContext.TraceIdentifier);

        return new BadRequestObjectResult(response);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. The 'Bearer ' prefix is added automatically."
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
            []
        }
    });

    options.OperationFilter<AnonymousOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var businessIndustrySeeder = scope.ServiceProvider.GetRequiredService<BusinessIndustrySeeder>();
    await businessIndustrySeeder.SeedAsync();

    var storeCategorySeeder = scope.ServiceProvider.GetRequiredService<StoreCategorySeeder>();
    await storeCategorySeeder.SeedAsync();

    var productCategorySeeder = scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>();
    await productCategorySeeder.SeedAsync();

    var staffRangeSeeder = scope.ServiceProvider.GetRequiredService<StaffRangeSeeder>();
    await staffRangeSeeder.SeedAsync();

    var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await roleSeeder.SeedAsync();

    var permissionSeeder = scope.ServiceProvider.GetRequiredService<PermissionSeeder>();
    await permissionSeeder.SeedAsync();

    var subscriptionPlanSeeder = scope.ServiceProvider.GetRequiredService<SubscriptionPlanSeeder>();
    await subscriptionPlanSeeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("LocalhostPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapControllers().RequireRateLimiting("fixed");

app.Run();
