using Microsoft.Extensions.DependencyInjection;

namespace Converter.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // TODO: register infrastructure services (repositories, FFmpeg adapters, notifications, etc.)
            return services;
        }
    }
}
