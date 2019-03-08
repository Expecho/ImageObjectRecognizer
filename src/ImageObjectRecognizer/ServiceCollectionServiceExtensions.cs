﻿using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ImageMetadataUpdater
{
    internal static class ServiceCollectionServiceExtensions
    {
        // Summary:
        //     Adds a singleton service of the type specified in serviceType with an implementation
        //     of the type with the name as specified in typeName to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        //
        // Parameters:
        //   services:
        //     The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the service
        //     to.
        //
        //   serviceType:
        //     The type of the service to register.
        //
        //   typeName:
        //     The name of the implementation type of the service.
        //
        // Returns:
        //     A reference to this instance after the operation has completed.
        public static IServiceCollection AddSingletonUsingTypeString<TService>(this IServiceCollection services, string typeName)
        {
            var implementationType = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetInterface(typeof(TService).FullName) != null)
                .FirstOrDefault(t => t.Name == typeName);

            services.AddSingleton(typeof(TService), implementationType);

            return services;
        }
    }
}