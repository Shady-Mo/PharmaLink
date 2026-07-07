namespace API;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices()
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddOpenApi();

            services
                .AddSwaggerServices()
                .AddCorsServices();


            return services;
        }

        private IServiceCollection AddCorsServices()
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

        private IServiceCollection AddSwaggerServices()
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "QuickBite API",
                        Version = "v1",
                        Contact = new OpenApiContact
                        {
                            Name = "QuickBite API Support",
                            Email = "support@quickbite.com"
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
    }
}