using FluentValidation;
using MediatR;

namespace EscrowApp.Features.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates all commands using FluentValidation.
/// Runs before any handler, throwing ValidationException if validation fails.
/// 
/// Registered in Program.cs:
///   cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators registered for this request type, skip validation
        if (!validators.Any())
            return await next();

        // Validate using all registered validators
        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        // If validation errors exist, throw exception (caught by ApiExceptionMiddleware)
        if (failures.Count > 0)
            throw new ValidationException(failures);

        // If valid, proceed to handler
        return await next();
    }
}
