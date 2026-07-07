using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tickets.Application.Behaviors;

namespace Tickets.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTicketsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
