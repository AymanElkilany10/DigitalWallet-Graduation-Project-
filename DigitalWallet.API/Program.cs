using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using DigitalWallet.Infrastructure.Data;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Infrastructure.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Services;
using DigitalWallet.Application.Helpers;
using DigitalWallet.API.Middleware;
using DigitalWallet.API.Filters;
using DigitalWallet.API.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DigitalWallet.Infrastructure.Data.Seeds;
using DigitalWallet.Application.ExternalServices.SMS;
using DigitalWallet.Application.ExternalServices.Email;
using DigitalWallet.Application.Settings;
using DigitalWallet.Application.Common.Models;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// SECTION 1: Service Configuration
// ═══════════════════════════════════════════════════════════════════════════

// ── 1.1 Database Context ────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly("DigitalWallet.Infrastructure")
    ));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));


// ── Redis Caching Configuration ────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "DigitalWallet_";
});

builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));
builder.Services.Configure<NotificationSettings>(builder.Configuration.GetSection("NotificationSettings"));




// ── 1.2 Repository Registration ────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<IBillPaymentRepository, BillPaymentRepository>();
builder.Services.AddScoped<IBillerRepository, BillerRepository>();
builder.Services.AddScoped<IFakeBankAccountRepository, FakeBankAccountRepository>();
builder.Services.AddScoped<IFakeBankTransactionRepository, FakeBankTransactionRepository>();
builder.Services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IMoneyRequestRepository, MoneyRequestRepository>();
builder.Services.AddScoped<IFraudLogRepository, FraudLogRepository>();


// ── 1.3 Service Registration ───────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IBillPaymentService, BillPaymentService>();
builder.Services.AddScoped<IFakeBankService, FakeBankService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IMoneyRequestService, MoneyRequestService>();
builder.Services.AddScoped<ICachingService, RedisCachingService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// External services
builder.Services.AddHttpClient<IExternalExchangeRateService, ExternalExchangeRateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});


// Currency exchange service
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();

// ── 1.4 JWT Token Generator ────────────────────────────────────────────────
builder.Services.AddScoped<JwtTokenGenerator>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    var secretKey = configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey not found");

    var issuer = configuration["Jwt:Issuer"] ?? "DigitalWallet.API";
    var audience = configuration["Jwt:Audience"] ?? "DigitalWallet.Clients";
    var expirationHours = int.Parse(configuration["Jwt:ExpirationHours"] ?? "24");

    return new JwtTokenGenerator(secretKey, issuer, audience, expirationHours);
});

// ── 1.5 JWT Authentication Configuration 🔥 CRITICAL - WAS MISSING! ─────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secretKey = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey is required");

    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DigitalWallet.API",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DigitalWallet.Clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Remove delay of token expiration
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(System.Security.Claims.ClaimTypes.Role, "SuperAdmin", "Support", "Auditor");
    });
});

// ── 1.6 AutoMapper ──────────────────────────────────────────────────────────
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ── 1.7 Controllers with Filters ────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// ── 1.8 CORS Configuration ──────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Correlation-Id");
    });
});

// ── 1.9 Swagger Configuration ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Digital Wallet API",
        Description = "RESTful API for Digital Wallet system with authentication, wallet management, transfers, and bill payments",
        Contact = new OpenApiContact
        {
            Name = "Digital Wallet Team",
            Email = "support@digitalwallet.com"
        }
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {token}"
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

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ── 1.10 Additional Services ────────────────────────────────────────────────
builder.Services.AddHttpClient();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database");

// ═══════════════════════════════════════════════════════════════════════════
// SECTION 2: Application Pipeline Configuration
// ═══════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// ── 2.1 Exception Handling (Outermost) ─────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ── 2.2 Request Logging ─────────────────────────────────────────────────────
app.UseMiddleware<RequestLoggingMiddleware>();

// ── 2.3 HTTPS Redirection ───────────────────────────────────────────────────
app.UseHttpsRedirection();

// ── 2.4 Routing ─────────────────────────────────────────────────────────────
app.UseRouting();

// ── 2.5 CORS (Must be AFTER UseRouting, BEFORE Auth) ───────────────────────
app.UseCors("AllowSpecificOrigins");

// ============================================================
// SEED DATA - ADD THIS SECTION
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Apply any pending migrations
        context.Database.Migrate();

        // Seed data
        DataSeeder.SeedData(context);

        Console.WriteLine("✅ Database seeded successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, " An error occurred while seeding the database.");
    }
}


// ── 2.6 Swagger (Development Only) ──────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Digital Wallet API V1");
        options.RoutePrefix = "swagger";
    });
}

// ── 2.7 Authentication & Authorization 🔥 CRITICAL ORDER! ───────────────────
// IMPORTANT: Do NOT use custom JwtAuthenticationMiddleware here
// The built-in UseAuthentication() handles JWT properly
app.UseAuthentication();   // Must come BEFORE UseAuthorization
app.UseAuthorization();

// ── 2.8 Map Controllers ─────────────────────────────────────────────────────
app.MapControllers();

// ── 2.9 Health Checks ───────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── 2.10 Root Endpoint ──────────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new
{
    Service = "Digital Wallet API",
    Version = "1.0.0",
    Status = "Running",
    Documentation = "/swagger",
    Health = "/health",
    Timestamp = DateTime.UtcNow
}));

// ═══════════════════════════════════════════════════════════════════════════
// SECTION 3: Application Startup
// ═══════════════════════════════════════════════════════════════════════════

app.Logger.LogInformation("Digital Wallet API starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Listening on: {Urls}", string.Join(", ", app.Urls));

app.Run();

app.Logger.LogInformation("Digital Wallet API stopped");