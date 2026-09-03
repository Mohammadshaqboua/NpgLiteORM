using System.Reflection;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Core;

public static class AttributeHelper
{
    public static string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        return columnAttribute == null ? property.Name : columnAttribute.Name;
    }
}
