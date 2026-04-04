namespace Vali_Flow.NoSql.IR;

/// <summary>Specifies whether a <see cref="NullNode"/> checks for null or non-null.</summary>
public enum NullCheckOp
{
    /// <summary>The field value must be <c>null</c> (<c>field == null</c>).</summary>
    IsNull,
    /// <summary>The field value must not be <c>null</c> (<c>field != null</c>).</summary>
    IsNotNull
}
