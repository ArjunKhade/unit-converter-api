namespace UnitConversion.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested unit category is not found.
/// </summary>
public sealed class CategoryNotFoundException : Exception
{
    public string Category { get; }

    public CategoryNotFoundException(string category)
        : base($"Measurement category '{category}' was not found.")
    {
        Category = category;
    }

    public CategoryNotFoundException(string category, string message)
        : base(message)
    {
        Category = category;
    }

    public CategoryNotFoundException(string category, string message, Exception innerException)
        : base(message, innerException)
    {
        Category = category;
    }
}
