using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ImageMetadataUpdater
{
    internal static class ServiceCollectionServiceExtensions
    {
        public static IServiceCollection AddSingletonUsingTypeString<TService>(this IServiceCollection services, string type)
        {
            var implementationType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetInterface(typeof(TService).FullName) != null)
                .FirstOrDefault(t => t.Name == type);

            services.AddSingleton(typeof(TService), implementationType);

            return services;
        }
    }
}