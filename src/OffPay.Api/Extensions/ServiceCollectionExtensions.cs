using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OffPay.Application.Abstractions;
using OffPay.Application.UseCases.Dispositivos.Commands;
using OffPay.Application.UseCases.Dispositivos.Queries;
using OffPay.Application.Validators;
using OffPay.Infrastructure.Auth;
using OffPay.Infrastructure.Persistence.Oracle;
using OffPay.Infrastructure.Persistence.Oracle.Repositories;

namespace OffPay.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OffPayDbContext>(options =>
            options.UseOracle(configuration.GetConnectionString("Oracle")));

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "OffPay — API de Auditoria & Compliance",
                Version = "v1",
                Description = "Microsservico .NET para auditoria criptografica de transacoes offline. " +
                              "Parte da plataforma OffPay, que viabiliza comercio em regioes com conectividade satelital intermitente (Starlink rural, zonas de desastre)."
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT do usuario (POST /api/auth/login)"
            };

            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repositórios
        services.AddScoped<IDispositivoRepository, DispositivoRepository>();
        services.AddScoped<ILogAuditoriaRepository, LogAuditoriaRepository>();

        // Auth services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IDeviceTokenService, DeviceTokenService>();

        // Use cases — Dispositivos
        services.AddScoped<RegistrarDispositivoHandler>();
        services.AddScoped<BloquearDispositivoHandler>();
        services.AddScoped<RevogarChavesDispositivoHandler>();
        services.AddScoped<ObterDispositivoHandler>();
        services.AddScoped<ListarDispositivosHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<RegistrarDispositivoRequestValidator>();

        return services;
    }
}
