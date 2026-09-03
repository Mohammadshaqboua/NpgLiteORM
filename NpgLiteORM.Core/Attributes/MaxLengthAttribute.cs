using System;

namespace NpgLiteORM.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MaxLengthAttribute : Attribute
{
    public int Length { get; }
    public MaxLengthAttribute(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than zero", nameof(length));
        Length = length;
    }
}