using FluentAssertions;
using Vali_Flow.Sql.Builder;
using Xunit;

namespace Vali_Flow.Sql.Tests;

/// <summary>
/// Tests for <see cref="SqlIdentifierGuard"/> — the SQL injection guard for table/schema/type names.
/// </summary>
public sealed class SqlIdentifierGuardTests
{
    [Theory]
    [InlineData("Users")]
    [InlineData("_users")]
    [InlineData("Users_2024")]
    [InlineData("dbo")]
    public void EnsureValidIdentifier_ValidName_ReturnsSameValue(string name)
    {
        var result = SqlIdentifierGuard.EnsureValidIdentifier(name, "tableName");

        result.Should().Be(name);
    }

    [Theory]
    [InlineData("Users]; DROP TABLE Users;--")]
    [InlineData("Users\"; DROP TABLE Users;--")]
    [InlineData("Users`; DROP TABLE Users;--")]
    [InlineData("Users WHERE 1=1")]
    [InlineData("Users; SELECT 1")]
    [InlineData("1Users")]
    [InlineData("Us ers")]
    public void EnsureValidIdentifier_UnsafeName_ThrowsArgumentException(string name)
    {
        var act = () => SqlIdentifierGuard.EnsureValidIdentifier(name, "tableName");

        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValidIdentifier_EmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => SqlIdentifierGuard.EnsureValidIdentifier(name!, "tableName");

        act.Should().Throw<ArgumentException>().WithParameterName("tableName");
    }

    [Theory]
    [InlineData("INT")]
    [InlineData("VARCHAR(50)")]
    [InlineData("DECIMAL(18,2)")]
    [InlineData("NVARCHAR(MAX)")]
    public void EnsureValidTypeName_ValidType_ReturnsSameValue(string typeName)
    {
        var result = SqlIdentifierGuard.EnsureValidTypeName(typeName, "typeName");

        result.Should().Be(typeName);
    }

    [Theory]
    [InlineData("INT); DROP TABLE Users;--")]
    [InlineData("VARCHAR(50); SELECT 1")]
    public void EnsureValidTypeName_UnsafeType_ThrowsArgumentException(string typeName)
    {
        var act = () => SqlIdentifierGuard.EnsureValidTypeName(typeName, "typeName");

        act.Should().Throw<ArgumentException>().WithParameterName("typeName");
    }
}
