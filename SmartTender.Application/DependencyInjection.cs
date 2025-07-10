using Microsoft.Extensions.DependencyInjection;
using SmartTender.Application.Interfaces;
using SmartTender.Application.Services;

namespace SmartTender.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITenderService, TenderService>();
            // Burada digər application servisləri də əlavə edə bilərsən
            return services;
        }
    }
} 