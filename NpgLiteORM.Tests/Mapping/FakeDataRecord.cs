using System.Data;

namespace NpgLiteORM.Tests.Mapping;

/// <summary>
/// Minimal in-memory stand-in for <see cref="IDataRecord"/>, used to unit-test
/// <see cref="NpgLiteORM.Core.Mapping.EntityMapper{T}.MapToEntity"/> without a real
/// database connection. Only the by-name indexer is implemented (what EntityMapper
/// actually uses); every other member throws since the tests never call them.
/// </summary>
public class FakeDataRecord : IDataRecord
{
    private readonly Dictionary<string, object> _data;

    /// <summary>
    /// Creates the fake row from a column-name → value dictionary. Lookups are
    /// case-insensitive, matching how real ADO.NET readers behave.
    /// </summary>
    /// <param name="data">Column values keyed by column name.</param>
    public FakeDataRecord(Dictionary<string, object> data)
    {
        _data = new Dictionary<string, object>(data, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Returns the value for the given column name — the only accessor EntityMapper actually needs.</summary>
    public object this[string name] => _data[name];

    /// <summary>Not used by EntityMapper; throws if called.</summary>
    public object this[int i] => throw new NotImplementedException();

    /// <summary>Number of columns in the fake row.</summary>
    public int FieldCount => _data.Count;

    // The members below are part of IDataRecord but are never exercised by
    // EntityMapper.MapToEntity (which only uses the by-name indexer), so they're
    // left unimplemented rather than fleshed out for a fake that doesn't need them.
    public string GetName(int i) => throw new NotImplementedException();
    public int GetOrdinal(string name) => throw new NotImplementedException();
    public object GetValue(int i) => throw new NotImplementedException();
    public int GetValues(object[] values) => throw new NotImplementedException();
    public string GetDataTypeName(int i) => throw new NotImplementedException();
    public Type GetFieldType(int i) => throw new NotImplementedException();
    public bool GetBoolean(int i) => throw new NotImplementedException();
    public byte GetByte(int i) => throw new NotImplementedException();
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
    public char GetChar(int i) => throw new NotImplementedException();
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotImplementedException();
    public IDataReader GetData(int i) => throw new NotImplementedException();
    public DateTime GetDateTime(int i) => throw new NotImplementedException();
    public decimal GetDecimal(int i) => throw new NotImplementedException();
    public double GetDouble(int i) => throw new NotImplementedException();
    public float GetFloat(int i) => throw new NotImplementedException();
    public Guid GetGuid(int i) => throw new NotImplementedException();
    public short GetInt16(int i) => throw new NotImplementedException();
    public int GetInt32(int i) => throw new NotImplementedException();
    public long GetInt64(int i) => throw new NotImplementedException();
    public string GetString(int i) => throw new NotImplementedException();
    public bool IsDBNull(int i) => throw new NotImplementedException();
}
