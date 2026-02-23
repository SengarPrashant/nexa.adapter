namespace LLMProviderAbstraction.Models;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation succeeded
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the collection of error messages from validation failures
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(bool success, IReadOnlyList<string> errors)
    {
        Success = success;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>A ValidationResult indicating success</returns>
    public static ValidationResult CreateSuccess()
    {
        return new ValidationResult(true, Array.Empty<string>());
    }

    /// <summary>
    /// Creates a failed validation result with error messages
    /// </summary>
    /// <param name="errors">Collection of validation error messages</param>
    /// <returns>A ValidationResult indicating failure with the provided errors</returns>
    public static ValidationResult CreateFailure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new ValidationResult(false, errorList.AsReadOnly());
    }
}
