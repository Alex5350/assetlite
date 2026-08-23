using AssetLite.Application.Abstractions;
using AssetLite.Application.Offices;
using AssetLite.Domain.Offices;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssetLite.Application;

/// <summary>
/// DI entry point for the Application layer. Call <c>services.AddApplication()</c> from the
/// host; ports (<see cref="IOfficeRepository"/>, <see cref="IAssetRepository"/>,
/// <see cref="ICategoryRepository"/>, <see cref="IAssetTagAllocator"/>, <see cref="IUnitOfWork"/>,
/// <see cref="IDateTimeProvider"/>, <see cref="IDomainEventDispatcher"/>) must be registered by
/// the Infrastructure layer.
/// </summary>
/// <remarks>
/// Handlers are resolved by their handler interfaces, e.g.
/// <c>ICommandHandler&lt;CreateOfficeCommand, OfficeDto&gt;</c> — no mediator package is used.
/// FluentValidation validators are registered here; run them at the API boundary (e.g. via
/// <c>builder.Services.AddValidation()</c> or explicit <c>IValidator&lt;T&gt;</c> invocation)
/// before dispatching to handlers.
/// </remarks>
public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaceDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    /// <summary>Registers validators, the office hierarchy domain service, and all handlers (scoped).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IOfficeHierarchy, OfficeHierarchy>();

        var registrations =
            from type in assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from handlerInterface in type.GetInterfaces()
            where handlerInterface.IsGenericType
                && HandlerInterfaceDefinitions.Contains(handlerInterface.GetGenericTypeDefinition())
            select (Implementation: type, Service: handlerInterface);

        foreach (var (implementation, service) in registrations)
        {
            services.AddScoped(service, implementation);
        }

        return services;
    }
}
