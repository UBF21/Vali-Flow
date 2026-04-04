namespace Vali_Flow.NoSql.IR;

/// <summary>Pattern-match operators for <see cref="LikeNode"/>.</summary>
public enum LikeOp
{
    /// <summary>The field value contains the pattern substring.</summary>
    Contains,
    /// <summary>The field value starts with the pattern.</summary>
    StartsWith,
    /// <summary>The field value ends with the pattern.</summary>
    EndsWith
}
