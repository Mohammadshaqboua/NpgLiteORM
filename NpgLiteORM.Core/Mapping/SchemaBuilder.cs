using System.Reflection;
using NpgLiteORM.Core.Attributes;
using NpgLiteORM.Core.Exceptions;

namespace NpgLiteORM.Core.Mapping;

/// <summary>
/// Turns an entity's attribute metadata ([Table], [Column], [PrimaryKey], etc.) into
/// PostgreSQL DDL. This is the only class in the library that knows how to generate
/// CREATE TABLE statements or resolve a table's physical name — every other component
/// (Repository, QueryBuilder, migrations) calls into this rather than duplicating the logic.
/// </summary>
public class SchemaBuilder
{
    /// <summary>
    /// Builds a full <c>CREATE TABLE IF NOT EXISTS</c> statement for entity type
    /// <typeparamref name="T"/>, generating one column definition per public property.
    /// </summary>
    /// <typeparam name="T">The entity type to generate DDL for.</typeparam>
    /// <returns>A complete, ready-to-execute CREATE TABLE statement.</returns>
    /// <exception cref="SchemaValidationException">Thrown if any property's .NET type has no known SQL mapping.</exception>
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
            // No [Table] attribute — fall back to the class name as-is.
            tableName = type.Name;
        }
        else
        {
            tableName = tableAttribute.Name;
        }
        return $"CREATE TABLE IF NOT EXISTS {tableName} ({columns});";
    }

    /// <summary>
    /// Resolves the physical table name for entity type <typeparamref name="T"/>: the
    /// name from <see cref="TableAttribute"/> if present, otherwise the class name.
    /// </summary>
    /// <typeparam name="T">The entity type to resolve a table name for.</typeparam>
    public string GetTableName<T>()
    {
        var type = typeof(T);
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        return tableAttribute == null ? type.Name : tableAttribute.Name;
    }

    /// <summary>
    /// Same resolution as <see cref="GetTableName{T}"/> but for a runtime <see cref="Type"/>
    /// instead of a generic parameter — used internally when resolving a foreign key's
    /// referenced table, where only a <see cref="Type"/> object is available.
    /// </summary>
    private string GetTableNameForType(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        return tableAttribute == null ? type.Name : tableAttribute.Name;
    }

    /// <summary>
    /// Builds one column's SQL definition ("name TYPE CONSTRAINTS") by combining its
    /// resolved column name, mapped SQL type, and every constraint-producing attribute
    /// present on the property ([PrimaryKey], [NotNull], [Unique], [ForeignKey], [MaxLength]).
    /// </summary>
    /// <param name="property">The reflected property to build a column definition for.</param>
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
            // [MaxLength] only matters for VARCHAR columns; every other SQL type ignores it.
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

    /// <summary>
    /// Maps a property's .NET type to the PostgreSQL column type to use for it, taking
    /// <see cref="Nullable{T}"/> unwrapping and <see cref="AutoIncrementAttribute"/>
    /// (SERIAL/BIGSERIAL vs plain INTEGER/BIGINT) into account.
    /// </summary>
    /// <param name="property">The reflected property whose type needs a SQL mapping.</param>
    /// <returns>The PostgreSQL column type, e.g. "INTEGER", "VARCHAR", "TIMESTAMP".</returns>
    /// <exception cref="SchemaValidationException">Thrown when the property's type has no known SQL mapping.</exception>
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

        // Anything else (enums, custom types, unsupported numerics, ...) has no known
        // mapping — fail loudly and specifically instead of generating broken DDL.
        throw new SchemaValidationException(
            property.DeclaringType!,
            property.Name,
            $"Type {underlyingType.Name} is not supported for property {property.Name}");
    }
}
