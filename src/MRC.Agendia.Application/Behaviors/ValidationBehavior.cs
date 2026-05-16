using FluentValidation;
using MediatR;

namespace MRC.Agendia.Application.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that runs all <see cref="IValidator{TRequest}"/>
    /// registered for the request type before the handler executes.
    ///
    /// If any validator fails, a <see cref="ValidationException"/> is thrown
    /// and the ExceptionHandlingMiddleware translates it to HTTP 400 with a
    /// structured payload: { code: "VALIDATION_ERROR", errors: { ... } }.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);

            return await next();
        }
    }
}
