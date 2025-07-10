using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartTender.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<SmartTenderDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Default")));

            // Burada digər infrastructure servisləri də əlavə edə bilərsən

            return services;
        }
    }
} 