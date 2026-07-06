using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;


namespace Infrastructure
{
    public static class DependancyInjection
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("cs");

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOp =>
                {
                    sqlOp.UseNetTopologySuite();
                })
            );

            services.AddIdentity<AppUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}
