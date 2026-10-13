using HighConvertAI.Server.Configuration;
using HighConvertAI.Server.Data;
using HighConvertAI.Server.Infrastructure;
using HighConvertAI.Server.Services;
using HighConvertAI.Server.Services.Abstractions;
using HighConvertAI.Server.Services.AiAnalysis;
using HighConvertAI.Server.Services.Auth;                              // 👈 NEW
using HighConvertAI.Server.Services.CatalogExtraction;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HighConvertAI.Server.Services.CatalogExtraction.Extractors;
using HighConvertAI.Server.Services.Email;
using HighConvertAI.Server.Services.HomepageExtraction;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;
using HighConvertAI.Server.Services.Otp;
using HighConvertAI.Server.Services.PageCrawler;
using HighConvertAI.Server.Services.PageDetection;
using HighConvertAI.Server.Services.ProductExtraction.Abstractions;
using HighConvertAI.Server.Services.ProductExtraction.Extractors;
using HighConvertAI.Server.Services.Screenshot;
using HighConvertAI.Server.Services.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "Logs", "highconvert-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


// =========================
// DATABASE
// =========================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));



// =========================
// CONTROLLERS + SWAGGER
// =========================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();



// =========================
// JWT AUTHENTICATION
// =========================

var jwt = builder.Configuration.GetSection("Jwt");                    // 👈 matches your "Jwt" key

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

            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt["Secret"]!)),

            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(context.Exception);
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine("Challenge: " + context.Error);
                Console.WriteLine(context.ErrorDescription);

                return Task.CompletedTask;
            }
        };
    });
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwt["Issuer"],
//        ValidAudience = jwt["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(
//                                       Encoding.UTF8.GetBytes(jwt["Secret"]!)),
//        ClockSkew = TimeSpan.Zero                      // No grace period on expiry
//    };
//});

builder.Services.AddAuthorization();



// =========================
// CORS
// =========================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://127.0.0.1:59745",
                "https://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// =========================
// HTTP CLIENTS
// =========================

builder.Services.AddHttpClient<IPageSpeedService, PageSpeedService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));



// =========================
// AUTH SERVICES                                                       👈 NEW
// =========================

builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();




// =========================
// APPLICATION SERVICES
// =========================

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddScoped<IPageCrawlerService, PlaywrightPageCrawlerService>();

builder.Services.AddScoped<IPageTypeDetectionService, ShopifyPageTypeDetectionService>();

builder.Services.AddScoped<IPageScanService, PageScanService>();
builder.Services.Configure<GoogleAiStudioOptions>(builder.Configuration.GetSection(GoogleAiStudioOptions.SectionName));
builder.Services.AddHttpClient<IAiProviderClient, GoogleAiStudioProviderClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddScoped<IAiPageScanAnalyzerService, AiPageScanAnalyzerService>();
builder.Services.AddScoped<IAiUrlScanAnalyzerService, AiUrlScanAnalyzerService>();
builder.Services.AddScoped<IProductPageExtractionService, ProductPageExtractionService>();

builder.Services.AddScoped<IHomepageExtractionService, HomepageExtractionService>();

builder.Services.AddScoped<ICatalogPageExtractionService, CatalogPageExtractionService>();

builder.Services.AddMemoryCache();



// =========================
// HOMEPAGE EXTRACTORS
// =========================

builder.Services.AddExtractionServices<IHomepageSectionExtractor>();



// =========================
// PRODUCT EXTRACTORS
// =========================

builder.Services.AddScoped<IProductSectionExtractor, BasicInfoExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, PricingExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, CtaExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, DescriptionExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, ShippingExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, TrustExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, ReviewsExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, FaqExtractor>();
builder.Services.AddScoped<IProductSectionExtractor, MediaExtractor>();



// =========================
// CATALOG EXTRACTORS
// =========================

builder.Services.AddScoped<ICatalogSectionExtractor, CatalogHeaderExtractor>();
builder.Services.AddScoped<ICatalogSectionExtractor, CatalogToolbarExtractor>();
builder.Services.AddScoped<ICatalogSectionExtractor, CatalogFiltersExtractor>();
builder.Services.AddScoped<ICatalogSectionExtractor, CatalogProductGridExtractor>();
builder.Services.AddScoped<ICatalogSectionExtractor, CatalogPaginationExtractor>();

builder.Services.AddScoped<IScreenshotService, ScreenshotService>();



var app = builder.Build();

app.UseExceptionHandler();


// =========================
// MIDDLEWARE PIPELINE
// =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AngularPolicy");

app.UseAuthentication();   // Must be before UseAuthorization

app.UseAuthorization();

app.MapControllers();

app.Run();