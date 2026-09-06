using System.Data;

namespace NpgLiteORM.Core.Mapping;

/// <summary>
/// The "Data Mapper" of the ORM: converts back and forth between a plain C# object
/// and a database row, using reflection over the entity's properties. Every public
/// property is mapped — there is no opt-out attribute yet, so all public properties
/// end up as columns.
/// </summary>
/// <typeparam name="T">Entity type being mapped. Needs a public parameterless constructor.</typeparam>
public class EntityMapper<T> where T : new()
{
    /// <summary>
    /// Converts an entity instance into a column-name → value dictionary, ready to be
    /// used as parameters for an INSERT or UPDATE statement. Reflects over every public
    /// property, resolving each one's column name via <see cref="AttributeHelper.GetColumnName"/>.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A dictionary of column name to the property's current value (values may be null).</returns>
    public Dictionary<string, object?> MapToRow(T entity)
    {
        var type = typeof(T);
        var row = new Dictionary<string, object?>();
        foreach (var property in type.GetProperties())
        {
            var columnName = AttributeHelper.GetColumnName(property);
            var value = property.GetValue(entity);
            row.Add(columnName, value);
        }
        return row;
    }

    /// <summary>
    /// Builds a new entity instance from a database row, reading each mapped property's
    /// column out of the <see cref="IDataRecord"/> by name.
    /// </summary>
    /// <param name="row">A single row from a data reader, positioned on the record to map.</param>
    /// <returns>A populated entity instance.</returns>
    public T MapToEntity(IDataRecord row)
    {
        var entity = new T();
        var type = typeof(T);

        foreach (var property in type.GetProperties())
        {
            var columnName = AttributeHelper.GetColumnName(property);
            var value = row[columnName];

            // A DBNull column is left at the property's default rather than assigned —
            // assigning DBNull.Value directly would throw for non-nullable value-type
            // properties (int, DateTime, ...), since they can't hold a null.
            if (value != DBNull.Value)
            {
                property.SetValue(entity, value);
            }
        }

        return entity;
    }
}
