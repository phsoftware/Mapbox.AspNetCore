using Mapbox.AspNetCore.Interfaces;
using Mapbox.AspNetCore.Models;
using Mapbox.AspNetCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mapbox.AspNetCore.DependencyInjection
{
    public static class MapboxServiceDependencyInjection
    {
        public static IServiceCollection AddMapBoxServices(this IServiceCollection services, Action<Options> optionsBuilder)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentException("Please use options to set the ApiKey");
            }

            var options = new Options();
            optionsBuilder(options);

            services.AddSingleton<IMapboxKeyService>(new MapboxKeyService(options.ApiKey));
            services.AddHttpClient<IMapboxService, MapBoxService>();

            return services;
        }
    }
}
