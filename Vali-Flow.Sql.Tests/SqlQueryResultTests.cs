using System.Data;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Tests.Models;

namespace Vali_Flow.Sql.Tests;

/// <summary>Tests for <see cref="Vali_Flow.Sql.Models.SqlQueryResult"/> and ToDebugSql.</summary>
public sealed class SqlQueryResultTests
{
    // ── Sql property ──────────────────────────────────────────────────────────

    [Fact]
    public void Sql_ContainsFullQuery()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Select(x => x.Id, x => x.Name)
            .Build();

        result.Sql.Should().Be("SELECT [Id], [Name] FROM [Users]");
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsSql()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Build();

        result.ToString().Should().Be(result.Sql);
    }

    // ── ToDebugSql ────────────────────────────────────────────────────────────

    [Fact]
    public void ToDebugSql_ReplacesIntParam()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.Age > 18)
            .Build();

        result.ToDebugSql().Should().Be("SELECT * FROM [Users] WHERE [Age] > 18");
    }

    [Fact]
    public void ToDebugSql_ReplacesStringParam_WithSingleQuotes()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.Name == "John")
            .Build();

        result.ToDebugSql().Should().Contain("'John'");
    }

    [Fact]
    public void ToDebugSql_MultipleParams_LongestFirst_NoPartialReplacement()
    {
        // 11 params (p0..p10) — ensures @p10 is replaced before @p1
        var ids = Enumerable.Range(0, 11).ToList();
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => ids.Contains(x.Id))
            .Build();

        result.ToDebugSql().Should().NotContain("@p");
    }

    [Fact]
    public void ToDebugSql_NullComparison_WritesIsNull()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.Name == null)
            .Build();

        result.ToDebugSql().Should().Be("SELECT * FROM [Users] WHERE [Name] IS NULL");
    }

    [Fact]
    public void ToDebugSql_DateTimeParam_Formatted()
    {
        var dt = new DateTime(2024, 1, 15, 10, 30, 0);
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.CreatedAt == dt)
            .Build();

        result.ToDebugSql().Should().Contain("'2024-01-15 10:30:00'");
    }

    // ── ApplyTo ───────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyTo_AddsParametersToCommand()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.Age > 18)
            .Build();

        var captured = new List<(string Name, object? Value)>();
        var cmd = new FakeDbCommand(captured);
        result.ApplyTo(cmd);

        captured.Should().HaveCount(1);
        captured[0].Name.Should().Be("p0");
        captured[0].Value.Should().Be(18);
    }

    [Fact]
    public void ApplyTo_NullCommand_ThrowsArgumentNull()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Build();

        var act = () => result.ApplyTo(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Parameters ────────────────────────────────────────────────────────────

    [Fact]
    public void Parameters_AreReadOnly()
    {
        var result = new SqlQueryBuilder<TestUser>(new SqlServerDialect())
            .From("Users")
            .Where((TestUser x) => x.Age > 18)
            .Build();

        result.Parameters.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>();
    }

    // ── Minimal IDbCommand stub ───────────────────────────────────────────────

    private sealed class FakeDbCommand(List<(string Name, object? Value)> captured) : IDbCommand
    {
        private readonly FakeParameterCollection _params = new(captured);

        public string CommandText { get; set; } = string.Empty;
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection? Connection { get; set; }
        public IDataParameterCollection Parameters => _params;
        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }
        public void Cancel() { }
        public IDbDataParameter CreateParameter() => new FakeParameter();
        public void Dispose() { }
        public int ExecuteNonQuery() => 0;
        public IDataReader ExecuteReader() => throw new NotSupportedException();
        public IDataReader ExecuteReader(CommandBehavior _) => throw new NotSupportedException();
        public object? ExecuteScalar() => null;
        public void Prepare() { }
    }

    private sealed class FakeParameterCollection(List<(string Name, object? Value)> captured)
        : IDataParameterCollection
    {
        private readonly List<IDataParameter> _items = new();

        public object this[string paramName]
        {
            get => _items.First(p => p.ParameterName == paramName);
            set => throw new NotSupportedException();
        }
        public object this[int index] { get => _items[index]; set => throw new NotSupportedException(); }
        public bool Contains(string paramName) => _items.Any(p => p.ParameterName == paramName);
        public int IndexOf(string paramName) => _items.FindIndex(p => p.ParameterName == paramName);
        public void RemoveAt(string paramName) => throw new NotSupportedException();
        public int Add(object value)
        {
            var p = (IDataParameter)value;
            _items.Add(p);
            captured.Add((p.ParameterName, p.Value == DBNull.Value ? null : p.Value));
            return _items.Count - 1;
        }
        public void Clear() => _items.Clear();
        public bool Contains(object? value) => value is IDataParameter p && _items.Contains(p);
        public void CopyTo(Array array, int index) => throw new NotSupportedException();
        public int Count => _items.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        public System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(object? value) => value is IDataParameter p ? _items.IndexOf(p) : -1;
        public void Insert(int index, object? value) => _items.Insert(index, (IDataParameter)value!);
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public void Remove(object? value) { if (value is IDataParameter p) _items.Remove(p); }
        public void RemoveAt(int index) => _items.RemoveAt(index);
    }

    private sealed class FakeParameter : IDbDataParameter
    {
        public string ParameterName { get; set; } = string.Empty;
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => false;
        public string SourceColumn { get; set; } = string.Empty;
        public DataRowVersion SourceVersion { get; set; }
    }
}
