namespace Vali_Flow.Classes.Evaluators;

/// <summary>
/// Generic transient-failure retry for bulk upsert/update operations, covering the concurrency pitfalls
/// <c>EFCore.BulkExtensions</c> itself documents for the most common relational providers:
/// <list type="bullet">
/// <item><description><b>PostgreSQL / MySQL</b>: matching by <c>BulkConfig.UpdateByProperties</c> (instead of
/// the primary key) makes the library create then drop a deterministically-named temporary unique
/// index/constraint around the merge. Two concurrent calls against the same table/key columns can race on
/// that shared object (one drops it mid-merge of the other), surfacing as e.g. PostgreSQL's
/// <c>42704 "index ... does not exist"</c>.</description></item>
/// <item><description><b>SQL Server</b>: the library's own README documents deadlocks as a known concurrency
/// issue for bulk merge operations (see upstream issue #46) — the server picks a victim transaction and kills
/// it with <c>"Transaction (Process ID ...) was deadlocked ... and has been chosen as the deadlock victim"</c>,
/// independent of the temp-table mechanism above (SQL Server's temp table names are randomized by default,
/// so they don't collide the way Postgres/MySQL's deterministic names do).</description></item>
/// <item><description><b>SQLite</b>: file-level write serialization means any concurrent writer (including a
/// second process) hitting the same database file while a bulk operation holds its write lock fails with
/// <c>SQLITE_BUSY "database is locked"</c> — a classic transient condition SQLite itself expects callers to
/// retry.</description></item>
/// </list>
/// These are the four most-used relational engines this ecosystem targets (see <c>Vali-Flow.Sql</c>'s
/// dialect support); Oracle is deliberately not covered here — it is supported by <c>EFCore.BulkExtensions</c>
/// but is a materially less common deployment target, and its concurrency failure wording hasn't been
/// characterized against this specific race.
/// The in-process <see cref="System.Threading.SemaphoreSlim"/> gate (see <c>ValiFlowEvaluator.Write.cs</c>,
/// <c>GetBulkUpsertGate</c>/<c>RunGatedBulkUpsertAsync</c>) already serializes same-process callers for the
/// first case, but neither case is covered once a second process (another instance of the API) is involved —
/// this retry is the generic, provider-agnostic fallback for that remaining window, for whichever of the
/// supported engines is in use.
/// </summary>
/// <remarks>
/// Deliberately detects transiency by inspecting the exception message chain for markers of these specific
/// failure modes, not by referencing a provider-specific exception type (<c>Npgsql</c>/<c>MySqlConnector</c>/
/// <c>Microsoft.Data.SqlClient</c>) — <c>Vali-Flow</c> has no package dependency on any ADO.NET provider and
/// this keeps it that way.
/// </remarks>
internal static class BulkUpsertRetryPolicy
{
    /// <summary>
    /// Default maximum attempts (1 initial try + up to 11 retries). History, measured against the real
    /// cross-process scenario this retry targets (2 API instances, 50 req/s combined, 15 shared business
    /// keys, <c>BulkInsertOrUpdateAsync</c> matching by a custom key):
    /// <list type="bullet">
    /// <item><description>4 attempts (~450ms budget): ~2% of requests (8/405) exhausted retries and propagated a 500.</description></item>
    /// <item><description>7 attempts (~2.4s budget): down to 0.12% (1/855) — real improvement, not zero.</description></item>
    /// <item><description>12 attempts (~5.4s budget, current): chosen to push further into this specific
    /// artificially-high-contention benchmark (15 keys shared by 2 processes at a sustained 50 req/s is a
    /// deliberately narrow pool, not representative of typical traffic). Zero residual failure under this
    /// exact scenario is not guaranteed by construction — the race is a queue, not a fixed number of
    /// contenders, so no finite retry budget can offer a 0% proof, only diminishing residual probability at
    /// the cost of added worst-case latency for the caller stuck re-trying.</description></item>
    /// </list>
    /// </summary>
    internal const int DefaultMaxAttempts = 12;

    /// <summary>Base delay for the exponential backoff between retries.</summary>
    internal static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Upper bound on any single computed delay, so backoff never grows unbounded. Raised alongside
    /// <see cref="DefaultMaxAttempts"/> — see that constant's remarks for the measured rationale.
    /// </summary>
    internal static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromMilliseconds(900);

    /// <summary>Upper bound (exclusive) of the random jitter added to each delay, to avoid a thundering herd of retries.</summary>
    internal const int DefaultJitterMs = 50;

    /// <summary>Base of the exponential backoff growth (delay doubles on each successive retry).</summary>
    private const double ExponentialBackoffBase = 2;

    // Substrings observed in the provider error message chain for the two concurrency failure modes above.
    private static readonly string[] TransientMarkers =
    {
        // PostgreSQL / MySQL — temp unique index/constraint race around a custom UpdateByProperties match.
        "tempUniqueIndex",      // the deterministic name EFCore.BulkExtensions gives the object it creates/drops
        "tempUniqueConstraint",
        "does not exist",       // PostgreSQL 42704 — one caller dropped the index between another's CREATE and its use
        "doesn't exist",        // MySQL wording for the equivalent race
        "unknown table",

        // SQL Server — deadlock victim on the bulk merge (documented upstream, EFCore.BulkExtensions issue #46).
        "deadlock",             // covers both "was deadlocked" and "deadlock victim" wording
        "deadlocked",

        // SQLite — SQLITE_BUSY: file-level write lock held by a concurrent writer (another process or thread).
        "database is locked",
        "sqlite_busy"
    };

    /// <summary>
    /// Walks the exception's <see cref="Exception.InnerException"/> chain looking for a message matching one
    /// of <see cref="TransientMarkers"/>. Returns <c>false</c> for anything else — this must stay narrow:
    /// a real constraint violation, a bad connection string, or any other failure should propagate immediately,
    /// not be silently retried and delayed.
    /// </summary>
    internal static bool IsTransientTempIndexFailure(Exception? ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            var message = current.Message;
            if (string.IsNullOrEmpty(message)) continue;

            foreach (var marker in TransientMarkers)
            {
                if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Runs <paramref name="operation"/>, retrying on failures classified as transient by
    /// <paramref name="isTransient"/> (defaults to <see cref="IsTransientTempIndexFailure"/>), with exponential
    /// backoff + jitter between attempts. Non-transient exceptions, and the last attempt's exception once
    /// <paramref name="maxAttempts"/> is exhausted, propagate unchanged — this never swallows a real failure.
    /// </summary>
    /// <param name="operation">The operation to run. Invoked at least once.</param>
    /// <param name="cancellationToken">
    /// Checked before every attempt and honored during the backoff delay via <see cref="Task.Delay(TimeSpan, CancellationToken)"/>;
    /// an <see cref="OperationCanceledException"/> from either path propagates immediately without being retried.
    /// </param>
    /// <param name="onRetry">
    /// Optional callback invoked right before each retry delay, with the 1-based attempt number that just
    /// failed and the exception that triggered the retry — used by the caller to tag diagnostics.
    /// </param>
    /// <param name="isTransient">Predicate deciding whether an exception should be retried. Defaults to <see cref="IsTransientTempIndexFailure"/>.</param>
    /// <param name="maxAttempts">Maximum total attempts, including the first. Must be at least 1.</param>
    /// <param name="baseDelay">Base delay for the exponential backoff (attempt 1 waits ~baseDelay, attempt 2 ~2x, etc.).</param>
    /// <param name="maxDelay">Upper bound applied to every computed delay before jitter is added.</param>
    internal static async Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken,
        Action<int, Exception>? onRetry = null,
        Func<Exception, bool>? isTransient = null,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts), "maxAttempts must be at least 1.");

        isTransient ??= IsTransientTempIndexFailure;
        var effectiveBaseDelay = baseDelay ?? DefaultBaseDelay;
        var effectiveMaxDelay = maxDelay ?? DefaultMaxDelay;

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await operation().ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts && isTransient(ex))
            {
                onRetry?.Invoke(attempt, ex);

                var exponential = effectiveBaseDelay.TotalMilliseconds * Math.Pow(ExponentialBackoffBase, attempt - 1);
                var capped = Math.Min(exponential, effectiveMaxDelay.TotalMilliseconds);
                var jitter = Random.Shared.Next(0, DefaultJitterMs);
                var delay = TimeSpan.FromMilliseconds(capped + jitter);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
