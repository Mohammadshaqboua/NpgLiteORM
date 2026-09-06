using System.Reflection;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Core;

/// <summary>
/// Centralizes the "what's this property's SQL column name?" lookup so it isn't
/// duplicated across <see cref="NpgLiteORM.Core.Mapping.EntityMapper{T}"/>,
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/>, and
/// <see cref="NpgLiteORM.Core.Query.ExpressionTranslator{T}"/>. If the column-naming
/// convention ever changes (e.g. auto snake_case), this is the one place to change it.
/// </summary>
public static class AttributeHelper
{
    /// <summary>
    /// Returns the SQL column name for a mapped property: the explicit name from
    /// <see cref="ColumnAttribute"/> if present, otherwise the property's own name as-is.
    /// </summary>
    /// <param name="property">The reflected property to resolve a column name for.</param>
    /// <returns>The column name to use in generated SQL.</returns>
    public static string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        return columnAttribute == null ? property.Name : columnAttribute.Name;
    }
}