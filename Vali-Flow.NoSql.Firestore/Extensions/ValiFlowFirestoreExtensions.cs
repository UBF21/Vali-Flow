using System.Linq.Expressions;
using Google.Cloud.Firestore;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Extensions;
using Vali_Flow.NoSql.Firestore.Translators;

namespace Vali_Flow.NoSql.Firestore.Extensions;

/// <summary>
/// Extension methods that add Google Cloud Firestore query-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="Filter"/> is the native Firestore SDK type — pass it directly to
/// <c>CollectionReference.Where(filter)</c> or <c>Query.Where(filter)</c>.
/// </remarks>
public static class ValiFlowFirestoreExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a native Firestore <see cref="Filter"/>.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <param name="customConverter">
    /// Optional override to convert a value into one Firestore accepts, for types the default conversion doesn't handle.
    /// </param>
    /// <returns>A <see cref="Filter"/> ready to pass to <c>Query.Where(filter)</c>.</returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;User&gt;()
    ///     .EqualTo(x => x.IsActive, true)
    ///     .GreaterThan(x => x.Age, 18);
    ///
    /// Filter firestoreFilter = filter.ToFirestore();
    /// var snapshot = await collection.Where(firestoreFilter).GetSnapshotAsync();
    /// </code>
    /// </example>
    public static Filter ToFirestore<T>(this ValiFlow<T> flow, Func<object?, object?>? customConverter = null, string? tag = null) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return FirestoreFilterTranslator.Translate(flow.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a native Firestore <see cref="Filter"/>.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static Filter ToFirestore<T>(this Expression<Func<T, bool>> expression, Func<object?, object?>? customConverter = null, string? tag = null)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return FirestoreFilterTranslator.Translate(expression.ToNoSqlIR(), customConverter, tag, typeof(T).Name);
    }
}
