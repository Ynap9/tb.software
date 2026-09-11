using Hangfire;
using InfisicalConfiguration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using NLog;
using NLog.Web;
using OpenIddict.Abstractions;
using System.Text;
using traobang.be.application.Auth.Implements;
using traobang.be.application.Auth.Interfaces;
using traobang.be.application.Base;
using traobang.be.application.ThongKe.Implements;
using traobang.be.application.ThongKe.Interfaces;
using traobang.be.application.TraoBang.Implements;
using traobang.be.application.TraoBang.Interface;
using traobang.be.application.TraoBang.Interfaces;
using traobang.be.domain.Auth;
using traobang.be.infrastructure.data;
using traobang.be.infrastructure.data.Seeder;
using traobang.be.infrastructure.external.BackgroundJob;
using traobang.be.infrastructure.external.Excel;
using traobang.be.infrastructure.external.File;
using traobang.be.infrastructure.external.File.Dtos;
using traobang.be.infrastructure.external.QrCode;
using traobang.be.infrastructure.external.SignalR.Hub.Implements;
using traobang.be.infrastructure.external.SignalR.Service.Implements;
using traobang.be.infrastructure.external.SignalR.Service.Interfaces;
using traobang.be.shared.Constants.Auth;
using traobang.be.shared.Settings;
using traobang.be.Workers;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Starting application...");
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("EnvironmentName => " + builder.Environment.EnvironmentName);

#region secret manager
if (builder.Environment.IsEnvironment("Staging") || builder.Environment.IsEnvironment("Production"))
{
    string infisicalEnv = builder.Environment.EnvironmentName switch
    {
        "Staging" => "staging",
        "Production" => "prod",
        _ => ""
    };

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddInfisical(
            new InfisicalConfigBuilder()
                .SetProjectId(Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID")!)
                .SetEnvironment(infisicalEnv)
                .SetInfisicalUrl(Environment.GetEnvironmentVariable("INFISICAL_URL")!) // your self-hosted URL
                .SetAuth(
                    new InfisicalAuthBuilder()
                        .SetUniversalAuth(
                            Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID")!,
                            Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET")!
                        )
                        .Build()
                )
                .Build()
        );
}
#endregion


builder.Logging.ClearProviders();
builder.Host.UseNLog();


#region db
string connectionString = builder.Configuration.GetConnectionString("TRAO_BANG")
    ?? throw new InvalidOperationException("Kh�ng t�m th?y connection string \"TRAO_BANG\" trong appsettings.json");

string hangfireConnectionString = builder.Configuration.GetConnectionString("HANGFIRE")
    ?? throw new InvalidOperationException("Kh�ng t�m th?y connection string \"HANGFIRE\" trong appsettings.json");

builder.Services.AddDbContext<TbDbContext>(options =>
{
    options.UseSqlServer(connectionString, options =>
    {
        //options.MigrationsAssembly(typeof(Program).Namespace);
        //options.MigrationsHistoryTable(DbSchemas.TableMigrationsHistory, DbSchemas.Core);
        options.CommandTimeout(600);
    });
    options.UseOpenIddict(); // Register OpenIddict entities
}, ServiceLifetime.Scoped);
#endregion


#region cors
string allowOrigins = builder.Configuration.GetSection("AllowedHosts")!.Value!;
//File.WriteAllText("cors.now.txt", $"CORS: {allowOrigins}");;/'\,
Console.WriteLine($"CORS: {allowOrigins}");
var origins = allowOrigins
    .Split(';')
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        ProgramExtensions.CorsPolicy,
        builder =>
        {
            builder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                //.AllowCredentials()
                .WithExposedHeaders("Content-Disposition");
        }
    );
    options.AddPolicy("SignalRPolicy", builder =>
    {
        builder
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
#endregion

#region identity
// 2. Add Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<TbDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});
#endregion

#region auth
string secretKey = builder.Configuration.GetSection("AuthServer:SecretKey").Value!;




builder.Services.Configure<AuthServerSettings>(builder.Configuration.GetSection("AuthServer"));



builder.Services.AddOpenIddict()
    .AddCore(opt =>
    {
        opt.UseEntityFrameworkCore()
           .UseDbContext<TbDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token")
            .SetAuthorizationEndpointUris("/connect/authorize")
        ;

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow()
                .AllowPasswordFlow()
                .AllowRefreshTokenFlow()
                .AllowAuthorizationCodeFlow()
                .RequireProofKeyForCodeExchange()
                ;

        options.AcceptAnonymousClients();
        options.DisableAccessTokenEncryption();

        options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.OfflineAccess, OpenIddictConstants.Scopes.Profile);

        // Register the signing and encryption credentials.
        //options.AddDevelopmentEncryptionCertificate()
        //       .AddDevelopmentSigningCertificate();

        // Development: ephemeral keys (or dev certs if you want)
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        // ?? Symmetric signing key
        var secret = Encoding.UTF8.GetBytes(secretKey);
        options.AddEncryptionKey(new SymmetricSecurityKey(secret));
        options.AddSigningKey(new SymmetricSecurityKey(secret));

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();

    });

builder.Services.AddAuthentication(options =>
{
    //options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,        // ? only check exp & nbf
                ClockSkew = TimeSpan.Zero, // no extra leeway

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                // Symmetric key (HMAC) example
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)
                ),

                // ?? Accept both "JWT" and "at+jwt" as token types
                ValidTypes = new[] { "JWT", "at+jwt" }
            };
            options.RequireHttpsMetadata = false;
        }
    )
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();


builder.Services.AddHostedService<AuthWorker>();
#endregion

#region mapper
// Build mapper configuration
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
#endregion

#region hangfire
builder.Services.ConfigureHangfire(hangfireConnectionString);
#endregion

#region signalr
builder.Services.AddSignalR();
builder.Services.AddScoped<ITraoBangService, TraoBangService>();
#endregion

#region s3
builder.Services.Configure<FileS3Config>(builder.Configuration.GetSection("FileS3Config"));

string endpoint = builder.Configuration.GetSection("FileS3Config:Endpoint").Value!;
string accessKey = builder.Configuration.GetSection("FileS3Config:AccessKey").Value!;
string s3SecretKey = builder.Configuration.GetSection("FileS3Config:SecretKey").Value!;

// Add Minio using the custom endpoint and configure additional settings for default MinioClient initialization
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(endpoint)
    .WithSSL(true)
    .WithCredentials(accessKey, s3SecretKey));

builder.Services.AddScoped<IFileS3Services, FileS3Services>();
#endregion

builder.Services.Configure<TemplateSettings>(builder.Configuration.GetSection("Template"));

#region qrcode
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
#endregion

// Add services to the container.
#region service
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionsService, PermissionsService>();

builder.Services.AddScoped<IExcelService, ExcelService>();

builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ISubPlanService, SubPlanService>();
builder.Services.AddScoped<ISlideService, SlideService>();
builder.Services.AddScoped<IGiaoDienService, GiaoDienService>();
builder.Services.AddScoped<IPrepareDataService, PrepareDataService>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();

#endregion

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

    // ?? Add Bearer JWT Security Definition
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\"",
    });

    // ?? Add Security Requirement (apply globally to all endpoints)
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#region Seed data
// Run seeding inside scope
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedUser.SeedAsync(userManager, roleManager);

}
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(ProgramExtensions.CorsPolicy);
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<TraoBangHub>("/hub/trao-bang").RequireCors("SignalRPolicy");
app.UseHangfireDashboard();
app.MapHealthChecks("/health");
app.Run();