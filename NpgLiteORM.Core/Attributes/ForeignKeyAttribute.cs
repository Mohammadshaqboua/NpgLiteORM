using System;

namespace NpgLiteORM.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public Type ReferencedType { get; }
    public string ReferencedColumn { get; }

    public ForeignKeyAttribute(Type referencedType, string referencedColumn = "id")
    {
        ReferencedType = referencedType;
        ReferencedColumn = referencedColumn;
    }
}