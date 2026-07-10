using API.Authorization;
using FluentValidation.AspNetCore;
using Infrastructure.Options;
using Infrastructure.Services.Order;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddFluentValidationAutoValidation();

        services.AddOpenApi();

        services
            .AddSwaggerServices()
            .AddCorsServices()
            .AddOrdersServices();

        return services;
    }

    private static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            var xmlFiles = new[]
            {
                "API.xml",
                "Application.xml"
            };

            foreach (var xmlFile in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }

            options.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "Pharma Link API",
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "Pharma Link API Support",
                        Email = "ziadhani64@gmail.com"
                    }
                });

            options.AddSecurityDefinition("bearer",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                { [new OpenApiSecuritySchemeReference("bearer", document)] = [] });
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthorization(
        this IServiceCollection services)
    {
        
        services.AddAuthorization();

        // this is for customizing authorization
        services.AddSingleton<IAuthorizationMiddlewareResultHandler,
            AppAuthorizationMiddlewareResultHandler>();

        return services;
    }

    public static IServiceCollection AddOrdersServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }


    public static IServiceCollection FulFillmentEngignService (this IServiceCollection services)
    {
        //moshady21
        services.AddScoped<IFulfillmentEngineService, FulfillmentEngineService>();

        return services;

    }

}