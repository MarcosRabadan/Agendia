using FluentValidation;
using FluentValidation.Results;

namespace MRC.Agendia.Tests.Unit.Application.Validators
{
    /// <summary>
    /// Small helpers to keep the validator tests terse: run the validator and assert
    /// on the property names that failed, using the plain <see cref="IValidator{T}"/>
    /// surface (no extra test-helper dependency).
    /// </summary>
    internal static class ValidatorTestExtensions
    {
        public static ValidationResult Check<T>(this IValidator<T> validator, T instance)
            => validator.Validate(instance);

        /// <summary>True when at least one failure targets <paramref name="propertyName"/>.</summary>
        public static bool Failed(this ValidationResult result, string propertyName)
            => result.Errors.Any(e => e.PropertyName == propertyName);

        public static void ShouldBeValid(this ValidationResult result)
            => Assert.True(result.IsValid,
                "Se esperaba valido pero fallo: " + string.Join(" | ",
                    result.Errors.Select(e => e.PropertyName + ": " + e.ErrorMessage)));

        /// <summary>Asserts the result is invalid and that <paramref name="propertyName"/> is among the failures.</summary>
        public static void ShouldFailOn(this ValidationResult result, string propertyName)
        {
            Assert.False(result.IsValid);
            Assert.True(result.Failed(propertyName),
                $"Se esperaba un error en '{propertyName}'. Errores: " + string.Join(" | ",
                    result.Errors.Select(e => e.PropertyName)));
        }
    }
}
