namespace UnitConversion.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested unit is not found or does not belong to the specified category.
/// </summary>
public sealed class UnitNotFoundException : Exception
{
    public string Unit { get; }
    public string Category { get; }

    public UnitNotFoundException(string unit, string category)
        : base($"Unit '{unit}' was not found in category '{category}'.")
    {
        Unit = unit;
        Category = category;
    }

    public UnitNotFoundException(string unit, string category, string message)
        : base(message)
    {
        Unit = unit;
        Category = category;
    }

    public UnitNotFoundException(string unit, string category, string message, Exception innerException)
        : base(message, innerException)
    {
        Unit = unit;
        Category = category;
    }
}
