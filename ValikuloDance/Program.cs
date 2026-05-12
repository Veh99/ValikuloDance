using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.Text;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Application.Services;
using ValikuloDance.Infrastructure.Data;

namespace ValikuloDance
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtSettings:Issuer"]!,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtSettings:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

            builder.Services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.Title = "Dance Studio API";
                config.Version = "v1";
                config.Description = "API для танцевальной студии";

                config.AddSecurity("Bearer", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Введите JWT токен. Пример: 'Bearer {token}'",
                    Name = "Authorization",
                    In = OpenApiSecurityApiKeyLocation.Header
                });

                config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    var allowedOrigins = builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>()
                        ?? ["http://localhost:3000", "https://localhost:3000"];

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dance");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
                options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
                options.EnableDetailedErrors(builder.Environment.IsDevelopment());
            });

            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<SubscriptionService>();
            builder.Services.AddScoped<ITelegramService, TelegramService>();
            builder.Services.AddHttpClient<IEmailService, EmailService>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ResourceService>();
            builder.Services.AddScoped<ITrainerService, TrainerService>();
            builder.Services.AddHostedService<TelegramPollingService>();
            builder.Services.AddHostedService<BookingStatusBackgroundService>();
            builder.Services.AddHostedService<SubscriptionMaintenanceBackgroundService>();
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.Configure<SubscriptionWorkflowSettings>(builder.Configuration.GetSection("SubscriptionWorkflow"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
            builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));

            var app = builder.Build();

            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();

            if (app.Environment.IsDevelopment())
            {
                app.UseOpenApi();
                app.UseSwaggerUi();
            }

            app.UseForwardedHeaders(forwardedHeadersOptions);
            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
