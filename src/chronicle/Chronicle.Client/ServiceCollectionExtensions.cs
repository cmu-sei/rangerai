// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle.Client.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chronicle.Client;

/// <summary>Puts the Chronicle client, and hosted subscribers, in an app's container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ChronicleClient"/>, bound to <paramref name="configuration"/>'s <c>Chronicle</c> section
    /// (<c>Url</c>, <c>App</c>, …). Returns the HttpClient builder, for handlers or resilience.
    /// </summary>
    public static IHttpClientBuilder AddChronicleClient(this IServiceCollection services, IConfiguration configuration) =>
        services.AddChronicleClient(o => configuration.GetSection(ChronicleClientOptions.Section).Bind(o));

    /// <summary>Registers <see cref="ChronicleClient"/>, configured in code. Returns the HttpClient builder.</summary>
    public static IHttpClientBuilder AddChronicleClient(this IServiceCollection services, Action<ChronicleClientOptions> configure)
    {
        services.Configure(configure);
        // A subscription is long-lived; the HttpClient's timeout covers only until its headers arrive.
        return services.AddHttpClient<ChronicleClient>(ChronicleClient.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Runs <typeparamref name="THandler"/> on every entry of one subscription, in the background, for as long as the
    /// app runs. Call it once per subscription, each with its own <paramref name="name"/>; register an
    /// <see cref="ICursorStore"/> to resume after a restart. Needs <see cref="AddChronicleClient(IServiceCollection, Action{ChronicleClientOptions})"/>.
    /// </summary>
    public static IServiceCollection AddChronicleSubscriber<THandler>(this IServiceCollection services, string name,
        Action<ChronicleSubscriberOptions> configure) where THandler : class, IChronicleHandler
    {
        services.Configure(name, configure);
        services.TryAddScoped<THandler>();
        services.AddSingleton<IHostedService>(sp => new ChronicleSubscriberService(
            name,
            sp,
            sp.GetRequiredService<ChronicleClient>(),
            sp.GetRequiredService<IOptionsMonitor<ChronicleSubscriberOptions>>(),
            sp.GetRequiredService<ILogger<ChronicleSubscriberService>>(),
            typeof(THandler)));
        return services;
    }

    /// <summary>
    /// Runs <typeparamref name="THandler"/> on one subscription bound from configuration, such as
    /// <c>Chronicle:Subscribers:{name}</c> with <c>ChronicleId</c>, <c>Audience</c>, <c>Viewers</c>, <c>Parts</c>.
    /// </summary>
    public static IServiceCollection AddChronicleSubscriber<THandler>(this IServiceCollection services, string name,
        IConfiguration section) where THandler : class, IChronicleHandler =>
        services.AddChronicleSubscriber<THandler>(name, o => section.Bind(o));
}
