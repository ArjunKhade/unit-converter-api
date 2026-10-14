namespace UnitConversion.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation fails during unit conversion (e.g. value below absolute zero).
/// </summary>
public sealed class UnitConversionValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public UnitConversionValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "Value", [message] }
        };
    }

    public UnitConversionValidationException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, [message] }
        };
    }

    public UnitConversionValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
