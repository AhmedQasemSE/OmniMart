using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OmniMart.API.Filters;
using OmniMart.API.Middlewares;
using OmniMart.Application.Behaviors;
using OmniMart.Application.Features.EventHandlers;
using OmniMart.Application.Features.Orders.EventHandlers;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Infrastructure.Data;
using OmniMart.Infrastructure.Data.Configurations;
using OmniMart.Infrastructure.Queries;
using OmniMart.Infrastructure.Repositories;
using OmniMart.Infrastructure.Security;
using OmniMart.Infrastructure.Security.Settings;
using OmniMart.Infrastructure.Services;
using OmniMart.Infrastructure.Services.BackgroundJobs;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Infrastructure.Services.Settings;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Required to retrieve the real client IP when hosted behind a reverse proxy or load balancer
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

// Configure Serilog for structured logging: Console, File (fallback), and Seq (live monitoring)
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("Logs/omnimart-log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Seq("http://localhost:5341")
);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

#region 1. Database Setup
builder.Services.AddScoped<DispatchDomainEventsInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>();

    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("OmniMart.Infrastructure"))
           .AddInterceptors(interceptor);
});
#endregion

#region 1.5 Background Jobs (Hangfire)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();
#endregion

#region 2. CQRS & MediatR
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "OmniMart_";
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(OmniMart.Application.AssemblyReference).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});
#endregion

#region 2.5 Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Partitioning by Remote IP Address to prevent abuse per user/client
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
#endregion

#region 3. Validation
builder.Services.AddValidatorsFromAssembly(typeof(OmniMart.Application.AssemblyReference).Assembly);
#endregion

#region 4. Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
builder.Services.AddScoped<IVendorProfileRepository, VendorProfileRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
builder.Services.AddScoped<IPaymentGroupRepository, PaymentGroupRepository>();
builder.Services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ICustomerRegisteredQueries, CustomerRegisteredQueries>();
builder.Services.AddScoped<IOrderCancellationQueries, OrderCancellationQueries>();
builder.Services.AddScoped<IOrderRefundedQueries, OrderRefundedQueries>();
builder.Services.AddScoped<IOrderPlacedEventQueries, OrderPlacedEventQueries>();
builder.Services.AddScoped<IOrderDeliveredQueries, OrderDeliveredQueries>();
builder.Services.AddScoped<IVendorRegisteredQueries, VendorRegisteredQueries>();
builder.Services.AddScoped<IPasswordResetQueries, PasswordResetQueries>();
builder.Services.AddScoped<IProductOutOfStockEventsQueries, ProductOutOfStockEventsQueries>();
builder.Services.AddScoped<IProductReviewedQueries, ProductReviewedQueries>();
builder.Services.AddScoped<IProductApprovedQueries, ProductApprovedQueries>();
builder.Services.AddScoped<IProductRejectedQueries, ProductRejectedQueries>();
builder.Services.AddScoped<IProductSuspendedQueries, ProductSuspendedQueries>();
builder.Services.AddScoped<IVendorRequiresReapprovalQueries, VendorRequiresReapprovalQueries>();
#endregion

#region 4.5 Health Checks
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OmniMart.Infrastructure.Data.AppDbContext>("OmniMart_Database")
    .AddRedis(redisConnection!);
#endregion

#region 5. Security & Authentication
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Essential for secure HttpOnly cookies transmission           
    });
});

builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
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
        ValidIssuer = jwtSettings!.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Retrieve JWT from HttpOnly Cookies for Hangfire Dashboard authentication
            var accessToken = context.Request.Cookies["AuthToken"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hangfire"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
#endregion

#region 6. Swagger Setup
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token here"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});
#endregion

#region 7. Global Error Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
#endregion

#region 8. HTTP Clients & Resilience (Polly)
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        // Exponential backoff: 2s, 4s, 8s
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine($"[Polly] External service call failed. Retrying... Attempt: {retryAttempt}");
        });

builder.Services.AddHttpClient("ExternalServicesClient")
    // Refresh connection every 5 minutes to ensure DNS/Network stability
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(retryPolicy);
#endregion

#region 9. External Services
builder.Services.Configure<OmniMart.Infrastructure.Services.Settings.CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<OmniMart.Application.Interfaces.IFileStorageService, CloudinaryService>();

builder.Services.Configure<OmniMart.Infrastructure.Services.Settings.StripeSettings>(
    builder.Configuration.GetSection("StripeSettings"));
builder.Services.AddScoped<OmniMart.Application.Interfaces.IPaymentService, OmniMart.Infrastructure.Services.StripePaymentService>();

builder.Services.Configure<OmniMart.Infrastructure.Services.Settings.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
#endregion

var app = builder.Build();

#region 10. Database Initialization & Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OmniMart.Infrastructure.Data.AppDbContext>();
        var passwordHasher = services.GetRequiredService<OmniMart.Application.Interfaces.Security.IPasswordHasherService>();

        var seeder = new OmniMart.Infrastructure.Data.DataSeeder(context, passwordHasher, app.Configuration);
        await seeder.SeedAdminAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the initial admin account.");
    }
}
#endregion

#region 11. HTTP Pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseForwardedHeaders();

// IMPORTANT: CORS must be placed before UseAuthentication to avoid preflight request issues
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

RecurringJob.AddOrUpdate<AbandonedCartCleanupJob>(
    "cleanup-abandoned-carts",
    job => job.ExecuteAsync(),
    Cron.MinuteInterval(5)
);

app.MapControllers();
app.MapHealthChecks("/api/health");
#endregion

app.Run();



