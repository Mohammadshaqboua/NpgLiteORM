using System.Reflection;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Core.Mapping;

public class SchemaBuilder
{
    public string BuildCreateTableSql<T>()
    {
        var type = typeof(T);
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        var properties = type.GetProperties();
        var columnDefinitions = new List<string>();
        foreach (var property in properties)
        {
            var columnDef = BuildColumnDefinition(property);
            columnDefinitions.Add(columnDef);
        }
        string columns = string.Join(", ", columnDefinitions);
        string tableName;
        if (tableAttribute == null)
        {
            tableName = type.Name;
        }
        else
        {
            tableName = tableAttribute.Name;
        }
        return $"CREATE TABLE IF NOT EXISTS {tableName} ({columns});";
    }

    public string GetTableName<T>()
    {
        var type = typeof(T);
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        return tableAttribute == null ? type.Name : tableAttribute.Name;
    }

    private string GetTableNameForType(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        return tableAttribute == null ? type.Name : tableAttribute.Name;
    }

    private string BuildColumnDefinition(PropertyInfo property)
    {
        string columnName = AttributeHelper.GetColumnName(property);
        string sqlType = MapCSharpTypeToSql(property);
        string constraints = "";
        if (property.IsDefined(typeof(PrimaryKeyAttribute)))
        {
            constraints += " PRIMARY KEY";
        }
        if (property.IsDefined(typeof(NotNullAttribute)))
        {
            constraints += " NOT NULL";
        }
        if (property.IsDefined(typeof(UniqueAttribute)))
        {
            constraints += " UNIQUE";
        }

        var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
        if (foreignKeyAttribute != null)
        {
            var referencedTableName = GetTableNameForType(foreignKeyAttribute.ReferencedType);
            constraints += $" REFERENCES {referencedTableName}({foreignKeyAttribute.ReferencedColumn})";
        }

        var maxLengthAttribute = property.GetCustomAttribute<MaxLengthAttribute>();
        if (sqlType == "VARCHAR")
        {
            if (maxLengthAttribute != null)
            {
                sqlType = $"VARCHAR({maxLengthAttribute.Length})";
            }
            else
            {
                sqlType = "VARCHAR(255)";
            }
        }
        return $"{columnName} {sqlType}{constraints}".TrimEnd();
    }

    private string MapCSharpTypeToSql(PropertyInfo property)
    {
        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        bool isAutoIncrement = property.IsDefined(typeof(AutoIncrementAttribute));

        if (underlyingType == typeof(int))
            return isAutoIncrement ? "SERIAL" : "INTEGER";
        if (underlyingType == typeof(long))
            return isAutoIncrement ? "BIGSERIAL" : "BIGINT";
        if (underlyingType == typeof(string))
            return "VARCHAR";
        if (underlyingType == typeof(bool))
            return "BOOLEAN";
        if (underlyingType == typeof(DateTime))
            return "TIMESTAMP";
        if (underlyingType == typeof(decimal))
            return "NUMERIC";
        if (underlyingType == typeof(double))
            return "DOUBLE PRECISION";
        if (underlyingType == typeof(Guid))
            return "UUID";

        throw new NotSupportedException($"Type {underlyingType.Name} not supported");
    }
}