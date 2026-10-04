using System.Text.RegularExpressions;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Validates raw SQL identifiers (table/schema names) and type names before they are interpolated
/// into generated SQL. Table/schema/type names cannot be parameterized like regular values
/// (there is no <c>@param</c> equivalent for an identifier), so this is the only line of defense
/// against SQL injection when a consumer builds these strings from untrusted input.
/// </summary>
internal static class SqlIdentifierGuard
{
    private static readonly Regex IdentifierPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    // Allows simple type names (INT, VARCHAR) and parameterized ones (DECIMAL(18,2), NVARCHAR(MAX)).
    private static readonly Regex TypeNamePattern =
        new(@"^[A-Za-z_][A-Za-z0-9_]*(\([A-Za-z0-9_,\s]+\))?$", RegexOptions.Compiled);

    /// <summary>Validates a table or schema name. Throws if it contains anything beyond letters, digits and underscores.</summary>
    public static string EnsureValidIdentifier(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Identifier cannot be empty.", paramName);
        if (!IdentifierPattern.IsMatch(name))
            throw new ArgumentException(
                $"'{name}' is not a valid SQL identifier. Only letters, digits and underscores are allowed " +
                "(table/schema names cannot be parameterized, so unsafe characters are rejected to prevent SQL injection).",
                paramName);
        return name;
    }

    /// <summary>Validates a SQL type name used in a CAST expression (e.g. "VARCHAR(50)", "DECIMAL(18,2)").</summary>
    public static string EnsureValidTypeName(string typeName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be empty.", paramName);
        if (!TypeNamePattern.IsMatch(typeName))
            throw new ArgumentException(
                $"'{typeName}' is not a valid SQL type name (expected something like 'INT' or 'VARCHAR(50)').",
                paramName);
        return typeName;
    }
}
