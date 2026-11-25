using Jellyfin.Sdk;
using JellyfinMPCShim.Interfaces;
using JellyfinMPCShim.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace JellyfinMPCShim.Extensions;

public static class ServiceExtension
{

    public static IServiceCollection AddJellyfin(this IServiceCollection services, string httpClientName = "Default")
    {
        services.AddSingleton<JellyfinSdkSettings>();
        services.AddSingleton<JellyfinClientIdentity>();
        services.AddSingleton<IAuthenticationProvider, ModernJellyfinAuthenticationProvider>();
        services.AddSingleton<JellyfinApiClient>();
        services.AddSingleton<IRequestAdapter, JellyfinRequestAdapter>(s => new JellyfinRequestAdapter(
            s.GetRequiredService<IAuthenticationProvider>(),
            s.GetRequiredService<JellyfinSdkSettings>(),
            s.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName)));
        services.AddSingleton<IJellyfinClient>(s => new JellyfinClient(
            s.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JellyfinClient>>(),
            s.GetRequiredService<JellyfinSdkSettings>(),
            s.GetRequiredService<JellyfinClientIdentity>(),
            s.GetRequiredService<JellyfinApiClient>(),
            s.GetRequiredService<IHttpClientFactory>(),
            httpClientName));
        return services;
    }

    public static IServiceCollection AddMpcClient(this IServiceCollection services)
    {
        return AddMpcClient(services, null);
    }

    public static IServiceCollection AddMpcClient(this IServiceCollection services, Action<MpcClientOptions>? options)
    {
        if (options != null)
        {
            services.Configure(options);
        }
        else
        {
            services.AddOptions<MpcClientOptions>();
        }
        services.AddSingleton<IMpcClient, MpcClient>();
        return services;
    }
}
