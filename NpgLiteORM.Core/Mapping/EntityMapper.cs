using System.Data;

namespace NpgLiteORM.Core.Mapping;

public class EntityMapper<T> where T : new()
{
    public Dictionary<string, object> MapToRow(T entity)
    {
        var type = typeof(T);
        var row = new Dictionary<string, object>();
        foreach (var property in type.GetProperties())
        {
            var columnName = AttributeHelper.GetColumnName(property);
            var value = property.GetValue(entity);
            row.Add(columnName, value);
        }
        return row;
    }
    
    public T MapToEntity(IDataRecord row)
    {
        var entity = new T();
        var type = typeof(T);

        foreach (var property in type.GetProperties())
        {
            var columnName = AttributeHelper.GetColumnName(property);
            var value = row[columnName];
        
            if (value != DBNull.Value)
            {
                property.SetValue(entity, value);
            }
        }

        return entity;
    }
}