using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Specifies the maximum character length for a string property.
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> uses this to generate
/// <c>VARCHAR(n)</c> instead of the default <c>VARCHAR(255)</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MaxLengthAttribute : Attribute
{
    /// <summary>Maximum allowed character length for the column.</summary>
    public int Length { get; }

    /// <summary>
    /// Creates the attribute with the given max length.
    /// </summary>
    /// <param name="length">Maximum length; must be greater than zero.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="length"/> is not positive.</exception>
    public MaxLengthAttribute(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero", nameof(length));
        Length = length;
    }
}