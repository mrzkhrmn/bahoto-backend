using Bahoto.Application.Interfaces;
using Bahoto.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bahoto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOilChangeService, OilChangeService>();
        return services;
    }
}
