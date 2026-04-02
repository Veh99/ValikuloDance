using ValikuloDance.Application.Interfaces;
using ValikuloDance.Application.Services;
using ValikuloDance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle;
using Microsoft.OpenApi;
using ValikuloDance.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ValikuloDance
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добавляем сервисы
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // для dev
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true
                };
            });

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Dance Studio API",
                    Version = "v1",
                    Description = "API для танцевальной студии"
                });
            });

            // Настройка CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Настройка базы данных
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

            // Регистрация сервисов
            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<ITelegramService, TelegramService>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ResourceService>();
            builder.Services.AddScoped<ITrainerService, TrainerService>();
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));


            var app = builder.Build();

            // Применяем миграции при запуске
            //using (var scope = app.Services.CreateScope())
            //{
            //    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //    try
            //    {
            //        await dbContext.Database.MigrateAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        throw;
            //    }
            //}

            // Настройка pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseAuthentication(); 
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
