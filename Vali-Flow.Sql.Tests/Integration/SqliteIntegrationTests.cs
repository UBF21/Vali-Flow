using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Tests.Integration;

internal sealed class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int IsActive { get; set; }
}

internal sealed class ProductWithOrder
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

internal sealed class OrderItemRow
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Integration tests using an in-memory SQLite database with Dapper and ADO.NET.</summary>
public sealed class SqliteIntegrationTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private static readonly ISqlDialect Dialect = new SqliteDialect();

    public SqliteIntegrationTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        CreateSchema();
        SeedData();
    }

    public void Dispose() => _conn.Dispose();

    private void CreateSchema()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Products (
                Id       INTEGER PRIMARY KEY,
                Name     TEXT    NOT NULL,
                Category TEXT    NOT NULL,
                Price    REAL    NOT NULL,
                Stock    INTEGER NOT NULL,
                IsActive INTEGER NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();

        using var cmd2 = _conn.CreateCommand();
        cmd2.CommandText = """
            CREATE TABLE OrderItems (
                Id        INTEGER PRIMARY KEY,
                ProductId INTEGER NOT NULL,
                Quantity  INTEGER NOT NULL
            )
            """;
        cmd2.ExecuteNonQuery();
    }

    private void SeedData()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Products (Id, Name, Category, Price, Stock, IsActive) VALUES
            (1, 'Apple',  'Fruit',  1.5, 100, 1),
            (2, 'Banana', 'Fruit',  0.8, 150, 1),
            (3, 'Carrot', 'Veggie', 0.5, 200, 1),
            (4, 'Donut',  'Sweet',  2.5,  50, 0),
            (5, 'Egg',    'Dairy',  3.0,  80, 1),
            (6, 'Fig',    'Fruit',  4.5,  30, 1),
            (7, 'Grape',  'Fruit',  3.2,  60, 0),
            (8, 'Honey',  'Sweet',  7.0,  20, 1)
            """;
        cmd.ExecuteNonQuery();

        using var cmd2 = _conn.CreateCommand();
        cmd2.CommandText = """
            INSERT INTO OrderItems (Id, ProductId, Quantity) VALUES
            (1, 1, 5),
            (2, 2, 3),
            (3, 1, 2),
            (4, 5, 1),
            (5, 3, 10)
            """;
        cmd2.ExecuteNonQuery();
    }

    private List<ProductRow> QueryDapper(SqlQueryResult result)
    {
        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        return _conn.Query<ProductRow>(result.Sql, dp).ToList();
    }

    private List<ProductRow> QueryAdoNet(SqlQueryResult result)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();
        var rows = new List<ProductRow>();
        while (reader.Read())
            rows.Add(new ProductRow
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                IsActive = reader.GetInt32(5)
            });
        return rows;
    }

    // ── Dapper tests ──────────────────────────────────────────────────────────

    [Fact]
    public void WhereEqualTo_Dapper_ReturnsFruitRows()
    {
        var where = new SqlWhereBuilder<ProductRow>().EqualTo(x => x.Category, "Fruit");
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().HaveCount(4);
        rows.Should().OnlyContain(r => r.Category == "Fruit");
    }

    [Fact]
    public void WhereGreaterThan_Dapper_ReturnsHighPriceRows()
    {
        var where = new SqlWhereBuilder<ProductRow>().GreaterThan(x => x.Price, 3.0m);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().OnlyContain(r => r.Price > 3.0m);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 6, 7, 8 });
    }

    [Fact]
    public void WhereBetween_Dapper_ReturnsMidPriceRows()
    {
        var where = new SqlWhereBuilder<ProductRow>().Between(x => x.Price, 1.0m, 3.0m);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().OnlyContain(r => r.Price >= 1.0m && r.Price <= 3.0m);
        // Apple=1.5, Donut=2.5, Egg=3.0 — Grape=3.2 is excluded
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 4, 5 });
    }

    [Fact]
    public void WhereIn_Dapper_ReturnsSpecifiedIds()
    {
        var where = new SqlWhereBuilder<ProductRow>().In(x => x.Id, new[] { 1, 3, 5 });
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().HaveCount(3);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 3, 5 });
    }

    [Fact]
    public void WhereContains_Dapper_ReturnsRowsWithEInName()
    {
        var where = new SqlWhereBuilder<ProductRow>().Contains(x => x.Name, "e");
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Name.Contains("e", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WhereOrGroup_Dapper_ReturnsFruitOrInactive()
    {
        var where = new SqlWhereBuilder<ProductRow>()
            .EqualTo(x => x.Category, "Fruit")
            .Or()
            .EqualTo(x => x.IsActive, 0);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Category == "Fruit" || r.IsActive == 0);
        // Donut (inactive, Sweet) + all Fruits (Apple, Banana, Fig, Grape)
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 2, 4, 6, 7 });
    }

    [Fact]
    public void WhereAndGroup_Dapper_ReturnsActiveFruits()
    {
        var where = new SqlWhereBuilder<ProductRow>()
            .EqualTo(x => x.Category, "Fruit")
            .EqualTo(x => x.IsActive, 1);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().OnlyContain(r => r.Category == "Fruit" && r.IsActive == 1);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 2, 6 });
    }

    [Fact]
    public void WhereSubGroup_Dapper_ReturnsNestedConditionRows()
    {
        // Id=1 OR (Price > 2.0 AND Stock > 50)
        var where = new SqlWhereBuilder<ProductRow>()
            .EqualTo(x => x.Id, 1)
            .Or()
            .AddSubGroup(g => g
                .GreaterThan(x => x.Price, 2.0m)
                .GreaterThan(x => x.Stock, 50));
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Id == 1 || (r.Price > 2.0m && r.Stock > 50));
    }

    [Fact]
    public void Pagination_Page1_ReturnsFirstThreeRows()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .OrderBy(x => x.Id)
            .Page(1, 3)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().HaveCount(3);
        rows.Select(r => r.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Pagination_Page2_ReturnsSecondPageRows()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .OrderBy(x => x.Id)
            .Page(2, 3)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().HaveCount(3);
        rows.Select(r => r.Id).Should().Equal(4, 5, 6);
    }

    [Fact]
    public void GroupByWithHavingCount_Dapper_ReturnsCategoriesWithMoreThanOneProduct()
    {
        var having = new SqlHavingBuilder<ProductRow>().CountGreaterThan(1);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Category\"")
            .SelectCount("cnt")
            .GroupBy(x => x.Category)
            .Having(having)
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();
        var categories = new List<string>();
        while (reader.Read())
            categories.Add(reader.GetString(0));

        categories.Should().NotBeEmpty();
        // Fruit (4), Sweet (2) have more than 1; Veggie (1) and Dairy (1) do not
        categories.Should().BeEquivalentTo(new[] { "Fruit", "Sweet" });
    }

    // ── ADO.NET tests ─────────────────────────────────────────────────────────

    [Fact]
    public void WhereEqualTo_AdoNet_ReturnsActiveRows()
    {
        var where = new SqlWhereBuilder<ProductRow>().EqualTo(x => x.IsActive, 1);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryAdoNet(result);
        rows.Should().HaveCount(6);
        rows.Should().OnlyContain(r => r.IsActive == 1);
    }

    [Fact]
    public void WhereGreaterThan_AdoNet_ReturnsHighStockRows()
    {
        var where = new SqlWhereBuilder<ProductRow>().GreaterThan(x => x.Stock, 60);
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(where)
            .Build();

        var rows = QueryAdoNet(result);
        rows.Should().OnlyContain(r => r.Stock > 60);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 2, 3, 5 });
    }

    [Fact]
    public void SelectColumns_AdoNet_ReadByIndex()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Select(x => x.Id, x => x.Name)
            .OrderBy(x => x.Id)
            .Take(3)
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();
        var ids = new List<int>();
        var names = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
            names.Add(reader.GetString(1));
        }

        ids.Should().Equal(1, 2, 3);
        names.Should().Equal("Apple", "Banana", "Carrot");
    }

    // ── Tag tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void Tag_PrependsSqlComment()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Tag("test")
            .Build();

        result.Sql.Should().StartWith("-- test\n");
    }

    [Fact]
    public void Tag_WritesToConsole_OnBuild()
    {
        string? logged = null;
        _ = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Tag("console test tag", msg => logged = msg)
            .Build();
        logged.Should().Be("[SQL] console test tag");
    }

    [Fact]
    public void Tag_SqlExecutesWithDapper()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Tag("dapper tag test")
            .Build();

        var rows = QueryDapper(result);
        rows.Should().HaveCount(8);
    }

    [Fact]
    public void Tag_Null_ThrowsArgumentException()
    {
        var act = () => new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Tag(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Tag_Whitespace_ThrowsArgumentException()
    {
        var act = () => new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Tag("   ");

        act.Should().Throw<ArgumentException>();
    }

    // ── INSERT integration tests ───────────────────────────────────────────────

    [Fact]
    public void Insert_SingleRow_InsertsRowCorrectly()
    {
        var result = new SqlInsertBuilder<ProductRow>(Dialect)
            .Into("Products")
            .Set(x => x.Id, 100)
            .Set(x => x.Name, "Kiwi")
            .Set(x => x.Category, "Fruit")
            .Set(x => x.Price, 2.0m)
            .Set(x => x.Stock, 40)
            .Set(x => x.IsActive, 1)
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        cmd.ExecuteNonQuery();

        var rows = _conn.Query<ProductRow>("SELECT * FROM Products WHERE Id = 100").ToList();
        rows.Should().HaveCount(1);
        rows[0].Name.Should().Be("Kiwi");
        rows[0].Category.Should().Be("Fruit");
    }

    [Fact]
    public void Insert_MultiRow_InsertsAllRows()
    {
        var result = new SqlInsertBuilder<ProductRow>(Dialect)
            .Into("Products")
            .Set(x => x.Id, 200).Set(x => x.Name, "Lemon").Set(x => x.Category, "Fruit").Set(x => x.Price, 1.2m).Set(x => x.Stock, 70).Set(x => x.IsActive, 1)
            .NextRow()
            .Set(x => x.Id, 201).Set(x => x.Name, "Mango").Set(x => x.Category, "Fruit").Set(x => x.Price, 3.5m).Set(x => x.Stock, 25).Set(x => x.IsActive, 1)
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        _conn.Execute(result.Sql, dp);

        var rows = _conn.Query<ProductRow>("SELECT * FROM Products WHERE Id IN (200, 201)").ToList();
        rows.Should().HaveCount(2);
        rows.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Lemon", "Mango" });
    }

    [Fact]
    public void Insert_WithReturning_ExecutesAndReturnsRow()
    {
        // SQLite 3.35+ supports RETURNING
        var result = new SqlInsertBuilder<ProductRow>(Dialect)
            .Into("Products")
            .Set(x => x.Id, 300)
            .Set(x => x.Name, "Nuts")
            .Set(x => x.Category, "Snack")
            .Set(x => x.Price, 5.0m)
            .Set(x => x.Stock, 10)
            .Set(x => x.IsActive, 1)
            .Returning()
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        var returned = _conn.Query<ProductRow>(result.Sql, dp).ToList();
        returned.Should().HaveCount(1);
        returned[0].Name.Should().Be("Nuts");
    }

    // ── UPDATE integration tests ───────────────────────────────────────────────

    [Fact]
    public void Update_WhereById_UpdatesCorrectRow()
    {
        var result = new SqlUpdateBuilder<ProductRow>(Dialect)
            .Table("Products")
            .Set(x => x.Price, 9.99m)
            .Set(x => x.IsActive, 0)
            .Where(w => w.EqualTo(x => x.Id, 1))
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        _conn.Execute(result.Sql, dp);

        var row = _conn.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = 1");
        row.Price.Should().Be(9.99m);
        row.IsActive.Should().Be(0);
    }

    [Fact]
    public void Update_InlineWhere_UpdatesMatchingRows()
    {
        var result = new SqlUpdateBuilder<ProductRow>(Dialect)
            .Table("Products")
            .Set(x => x.IsActive, 0)
            .Where(w => w.EqualTo(x => x.Category, "Sweet"))
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        _conn.Execute(result.Sql, dp);

        var sweetRows = _conn.Query<ProductRow>("SELECT * FROM Products WHERE Category = 'Sweet'").ToList();
        sweetRows.Should().OnlyContain(r => r.IsActive == 0);
    }

    [Fact]
    public void Update_WithReturning_ReturnsUpdatedRows()
    {
        var result = new SqlUpdateBuilder<ProductRow>(Dialect)
            .Table("Products")
            .Set(x => x.Stock, 999)
            .Where(w => w.EqualTo(x => x.Id, 3))
            .Returning()
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        var returned = _conn.Query<ProductRow>(result.Sql, dp).ToList();
        returned.Should().HaveCount(1);
        returned[0].Stock.Should().Be(999);
    }

    // ── DELETE integration tests ───────────────────────────────────────────────

    [Fact]
    public void Delete_WhereById_RemovesCorrectRow()
    {
        var result = new SqlDeleteBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Id, 8))
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        _conn.Execute(result.Sql, dp);

        var rows = _conn.Query<ProductRow>("SELECT * FROM Products WHERE Id = 8").ToList();
        rows.Should().BeEmpty();
    }

    [Fact]
    public void Delete_InlineWhere_RemovesMatchingRows()
    {
        var result = new SqlDeleteBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.IsActive, 0))
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        _conn.Execute(result.Sql, dp);

        var remaining = _conn.Query<ProductRow>("SELECT * FROM Products WHERE IsActive = 0").ToList();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public void Delete_WithReturning_ReturnsDeletedRows()
    {
        var result = new SqlDeleteBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Id, 7))
            .Returning()
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);
        var deleted = _conn.Query<ProductRow>(result.Sql, dp).ToList();
        deleted.Should().HaveCount(1);
        deleted[0].Id.Should().Be(7);
    }

    // ── CTE integration test ───────────────────────────────────────────────────

    [Fact]
    public void WithCte_Dapper_ExecutesCorrectly()
    {
        // WITH FruitProducts AS (SELECT * FROM Products WHERE Category = 'Fruit')
        // SELECT * FROM FruitProducts WHERE Price > 2.0
        var cteQuery = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .WithCte("FruitProducts", cteQuery)
            .From("FruitProducts")
            .Where(w => w.GreaterThan(x => x.Price, 2.0m))
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Category == "Fruit" && r.Price > 2.0m);
    }

    // ── WhereInSubquery integration test ──────────────────────────────────────

    [Fact]
    public void WhereInSubquery_Dapper_ReturnsMatchingRows()
    {
        // SELECT * FROM Products WHERE Id IN (SELECT Id FROM Products WHERE Category = 'Fruit')
        var sub = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Select(x => x.Id)
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .WhereInSubquery(x => x.Id, sub)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.Category == "Fruit");
    }

    [Fact]
    public void WhereNotInSubquery_Dapper_ExcludesMatchingRows()
    {
        var sub = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Select(x => x.Id)
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .WhereNotInSubquery(x => x.Id, sub)
            .Build();

        var rows = QueryDapper(result);
        rows.Should().NotBeEmpty();
        rows.Should().NotContain(r => r.Category == "Fruit");
    }

    // ── JOIN integration tests ────────────────────────────────────────────────

    [Fact]
    public void InnerJoin_Dapper_ReturnsOnlyMatchingRows()
    {
        // SELECT Products.Id, Products.Name, OrderItems.Quantity
        // FROM Products INNER JOIN OrderItems ON Products.Id = OrderItems.ProductId
        // Only products that have at least one order: Id 1, 2, 3, 5
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Products\".\"Id\"")
            .SelectRaw("\"Products\".\"Name\"")
            .SelectRaw("\"OrderItems\".\"Quantity\"")
            .InnerJoin("OrderItems", null, "\"Products\".\"Id\" = \"OrderItems\".\"ProductId\"")
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);

        var rows = _conn.Query<ProductWithOrder>(result.Sql, dp).ToList();

        // 5 order items total, joined to their products
        rows.Should().HaveCount(5);
        rows.Should().OnlyContain(r => new[] { 1, 2, 3, 5 }.Contains(r.Id));
        rows.Should().OnlyContain(r => r.Quantity > 0);
    }

    [Fact]
    public void LeftJoin_Dapper_ReturnsAllProductsIncludingNoOrders()
    {
        // SELECT "Products"."Id", "Products"."Name", COUNT("OrderItems"."Id") AS "Quantity"
        // FROM "Products" LEFT JOIN "OrderItems" ON "Products"."Id" = "OrderItems"."ProductId"
        // GROUP BY "Products"."Id", "Products"."Name"
        // All 8 products are returned; those without orders get COUNT = 0
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Products\".\"Id\"")
            .SelectRaw("\"Products\".\"Name\"")
            .SelectRaw("COUNT(\"OrderItems\".\"Id\") AS \"Quantity\"")
            .LeftJoin("OrderItems", null, "\"Products\".\"Id\" = \"OrderItems\".\"ProductId\"")
            .GroupByRaw("\"Products\".\"Id\", \"Products\".\"Name\"")
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);

        var rows = _conn.Query<ProductWithOrder>(result.Sql, dp).ToList();

        rows.Should().HaveCount(8);
        // Products with orders (1, 2, 3, 5) have Quantity > 0
        rows.Where(r => new[] { 1, 2, 3, 5 }.Contains(r.Id))
            .Should().OnlyContain(r => r.Quantity > 0);
        // Products without orders (4, 6, 7, 8) have Quantity == 0
        rows.Where(r => new[] { 4, 6, 7, 8 }.Contains(r.Id))
            .Should().OnlyContain(r => r.Quantity == 0);
    }

    [Fact]
    public void InnerJoin_AdoNet_ReturnsMatchingRows()
    {
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Products\".\"Id\"")
            .SelectRaw("\"Products\".\"Name\"")
            .SelectRaw("\"OrderItems\".\"Quantity\"")
            .InnerJoin("OrderItems", null, "\"Products\".\"Id\" = \"OrderItems\".\"ProductId\"")
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();

        var rows = new List<ProductWithOrder>();
        while (reader.Read())
            rows.Add(new ProductWithOrder
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Quantity = reader.GetInt32(2)
            });

        rows.Should().HaveCount(5);
        rows.Should().OnlyContain(r => new[] { 1, 2, 3, 5 }.Contains(r.Id));
    }

    // ── UNION / UNION ALL integration tests ───────────────────────────────────

    [Fact]
    public void Union_Dapper_DeduplicatesRows()
    {
        // Two queries selecting the same Fruit rows — UNION should deduplicate
        var q1 = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var q2 = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Union(q2)
            .Build();

        var rows = QueryDapper(result);

        // UNION deduplicates — still 4 unique Fruit rows
        rows.Should().HaveCount(4);
        rows.Should().OnlyContain(r => r.Category == "Fruit");
    }

    [Fact]
    public void UnionAll_Dapper_IncludesDuplicates()
    {
        // UNION ALL between two identical queries — should return double the rows
        var q2 = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.Category, "Fruit"))
            .UnionAll(q2)
            .Build();

        var rows = QueryDapper(result);

        // UNION ALL: 4 Fruit rows × 2 = 8 rows
        rows.Should().HaveCount(8);
        rows.Should().OnlyContain(r => r.Category == "Fruit");
    }

    // ── DISTINCT integration tests ─────────────────────────────────────────────

    [Fact]
    public void Distinct_Dapper_ReturnsUniqueCategories()
    {
        // SELECT DISTINCT Category FROM Products
        // Should return 4 unique categories: Fruit, Veggie, Sweet, Dairy
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Category\"")
            .Distinct()
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();

        var categories = new List<string>();
        while (reader.Read())
            categories.Add(reader.GetString(0));

        categories.Should().HaveCount(4);
        categories.Should().BeEquivalentTo(new[] { "Fruit", "Veggie", "Sweet", "Dairy" });
    }

    // ── CASE WHEN integration tests ───────────────────────────────────────────

    [Fact]
    public void SelectCase_Dapper_MapsCategoriesToLabels()
    {
        // CASE WHEN "Category" = 'Fruit' THEN 'Plant' ELSE 'Other' END AS "Label"
        var caseExpr = CaseWhenBuilder
            .Create("\"Category\" = 'Fruit'", "Plant")
            .Else("Other")
            .As("Label");

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectRaw("\"Id\"")
            .SelectRaw("\"Category\"")
            .SelectCase(caseExpr)
            .OrderBy(x => x.Id)
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();

        var labels = new List<(int Id, string Category, string Label)>();
        while (reader.Read())
            labels.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));

        labels.Should().HaveCount(8);
        labels.Where(r => r.Category == "Fruit").Should().OnlyContain(r => r.Label == "Plant");
        labels.Where(r => r.Category != "Fruit").Should().OnlyContain(r => r.Label == "Other");
    }

    // ── Aggregate SELECT integration tests ────────────────────────────────────

    [Fact]
    public void SelectSum_Dapper_ReturnsTotalStock()
    {
        // SELECT SUM(Stock) AS TotalStock FROM Products WHERE IsActive = 1
        // Active products: Apple(100), Banana(150), Carrot(200), Egg(80), Fig(30), Honey(20) = 580
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectSum(x => x.Stock, "TotalStock")
            .Where(w => w.EqualTo(x => x.IsActive, 1))
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);

        var total = _conn.ExecuteScalar<long>(result.Sql, dp);
        total.Should().Be(580L);
    }

    [Fact]
    public void SelectAvg_Dapper_ReturnsAveragePrice()
    {
        // SELECT AVG(Price) AS AvgPrice FROM Products
        // (1.5 + 0.8 + 0.5 + 2.5 + 3.0 + 4.5 + 3.2 + 7.0) / 8 = 23.0 / 8 = 2.875
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectAvg(x => x.Price, "AvgPrice")
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);

        var avg = _conn.ExecuteScalar<double>(result.Sql, dp);
        avg.Should().BeApproximately(2.875, 0.001);
    }

    [Fact]
    public void SelectMin_SelectMax_Dapper_ReturnsMinMaxPrice()
    {
        // SELECT MIN(Price) AS MinPrice, MAX(Price) AS MaxPrice FROM Products
        // Min = 0.5 (Carrot), Max = 7.0 (Honey)
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectMin(x => x.Price, "MinPrice")
            .SelectMax(x => x.Price, "MaxPrice")
            .Build();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = result.Sql;
        result.ApplyTo(cmd);
        using var reader = cmd.ExecuteReader();
        reader.Read();

        var minPrice = reader.GetDouble(0);
        var maxPrice = reader.GetDouble(1);

        minPrice.Should().BeApproximately(0.5, 0.001);
        maxPrice.Should().BeApproximately(7.0, 0.001);
    }

    [Fact]
    public void SelectCount_Column_Dapper_CountsNonNullValues()
    {
        // SELECT COUNT("Name") FROM Products
        // All 8 products have non-null Name values
        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .SelectCount(x => x.Name, "NameCount")
            .Build();

        var dp = new DynamicParameters();
        foreach (var (key, value) in result.Parameters)
            dp.Add(key, value);

        var count = _conn.ExecuteScalar<long>(result.Sql, dp);
        count.Should().Be(8L);
    }

    // ── Subquery FROM integration test ────────────────────────────────────────

    [Fact]
    public void FromSubquery_Dapper_FiltersOnSubqueryResult()
    {
        // SELECT * FROM (SELECT * FROM Products WHERE IsActive = 1) sub WHERE Price > 2.0
        // Active products with Price > 2.0: Egg(3.0), Fig(4.5), Honey(7.0) → Ids 5, 6, 8
        var inner = new SqlQueryBuilder<ProductRow>(Dialect)
            .From("Products")
            .Where(w => w.EqualTo(x => x.IsActive, 1))
            .Build();

        var result = new SqlQueryBuilder<ProductRow>(Dialect)
            .From(inner, "sub")
            .Where(w => w.GreaterThan(x => x.Price, 2.0m))
            .Build();

        var rows = QueryDapper(result);

        rows.Should().HaveCount(3);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { 5, 6, 8 });
        rows.Should().OnlyContain(r => r.IsActive == 1 && r.Price > 2.0m);
    }
}
