using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace MediatX;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatX(this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddSingleton(new MediatorConfig(assemblies));

        if (assemblies.Length == 0)
            return services;

        RegisterHandlers(services, assemblies);
        RegisterBehaviors(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerTypes = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(IRequestHandler<>),
            typeof(INotificationHandler<>)
        };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                handlerTypes.Contains(i.GetGenericTypeDefinition()))
                    .ToList();

                foreach (var i in interfaces)
                {
                    services.AddTransient(i, type);
                }
            }
        }
    }

    private static void RegisterBehaviors(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))))
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

                foreach (var i in interfaces)
                {
                    services.AddTransient(i, type);
                }
            }
        }
    }
}

internal sealed class MediatorConfig
{
    public Assembly[] Assemblies { get; }
    public MediatorConfig(Assembly[] assemblies) => Assemblies = assemblies;
}