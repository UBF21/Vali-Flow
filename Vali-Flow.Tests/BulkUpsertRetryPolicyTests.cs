using FluentAssertions;
using Vali_Flow.Classes.Evaluators;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Unit tests for <see cref="BulkUpsertRetryPolicy"/> in isolation — no EF Core/database involved, since the
/// race it retries (concurrent bulk upsert temp-index/deadlock/busy-file failures) can't be reproduced
/// deterministically against the InMemory provider. Exercises the retry loop and transient-detection logic
/// directly via injected failing/succeeding operations and synthetic exceptions.
/// </summary>
public sealed class BulkUpsertRetryPolicyTests
{
    // ── IsTransientTempIndexFailure ──────────────────────────────────────────

    [Theory]
    [InlineData("42704: index \"tempUniqueIndex_public_orders_sku\" does not exist")]
    [InlineData("relation \"tempUniqueConstraint_x\" does not exist")]
    [InlineData("Table 'db.tempUniqueConstraint_orders' doesn't exist")]
    [InlineData("Unknown table 'tempUniqueIndex_orders' in information_schema")]
    [InlineData("Transaction (Process ID 61) was deadlocked on lock resources with another process and has been chosen as the deadlock victim")]
    [InlineData("database is locked")]
    [InlineData("SQLite Error 5: 'database is locked'")]
    public void IsTransientTempIndexFailure_KnownEngineMarkers_ReturnsTrue(string message)
    {
        var ex = new InvalidOperationException(message);

        BulkUpsertRetryPolicy.IsTransientTempIndexFailure(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransientTempIndexFailure_MarkerInInnerException_ReturnsTrue()
    {
        var inner = new InvalidOperationException("42704: index \"tempUniqueIndex_x\" does not exist");
        var outer = new Exception("Bulk operation failed", inner);

        BulkUpsertRetryPolicy.IsTransientTempIndexFailure(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("duplicate key value violates unique constraint \"orders_pkey\"")]
    [InlineData("Column 'Name' cannot be null")]
    [InlineData("Connection string is invalid")]
    public void IsTransientTempIndexFailure_UnrelatedFailures_ReturnsFalse(string message)
    {
        var ex = new InvalidOperationException(message);

        BulkUpsertRetryPolicy.IsTransientTempIndexFailure(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransientTempIndexFailure_Null_ReturnsFalse()
    {
        BulkUpsertRetryPolicy.IsTransientTempIndexFailure(null).Should().BeFalse();
    }

    // ── ExecuteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SucceedsFirstTry_RunsOperationOnceAndDoesNotRetry()
    {
        var callCount = 0;
        var retryCount = 0;

        await BulkUpsertRetryPolicy.ExecuteAsync(
            () => { callCount++; return Task.CompletedTask; },
            CancellationToken.None,
            onRetry: (_, _) => retryCount++);

        callCount.Should().Be(1);
        retryCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_TransientFailureThenSuccess_RetriesAndSucceeds()
    {
        var callCount = 0;
        var retriedAttempts = new List<int>();

        await BulkUpsertRetryPolicy.ExecuteAsync(
            () =>
            {
                callCount++;
                if (callCount < 3) throw new InvalidOperationException("database is locked");
                return Task.CompletedTask;
            },
            CancellationToken.None,
            onRetry: (attempt, _) => retriedAttempts.Add(attempt),
            baseDelay: TimeSpan.FromMilliseconds(1),
            maxDelay: TimeSpan.FromMilliseconds(5));

        callCount.Should().Be(3);
        retriedAttempts.Should().Equal(1, 2);
    }

    [Fact]
    public async Task ExecuteAsync_NonTransientFailure_ThrowsImmediatelyWithoutRetrying()
    {
        var callCount = 0;

        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(
            () => { callCount++; throw new InvalidOperationException("Column 'Name' cannot be null"); },
            CancellationToken.None,
            baseDelay: TimeSpan.FromMilliseconds(1));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Column 'Name' cannot be null");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_TransientFailureExhaustsMaxAttempts_ThrowsLastException()
    {
        var callCount = 0;

        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(
            () => { callCount++; throw new InvalidOperationException("database is locked"); },
            CancellationToken.None,
            maxAttempts: 3,
            baseDelay: TimeSpan.FromMilliseconds(1),
            maxDelay: TimeSpan.FromMilliseconds(5));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("database is locked");
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_CustomIsTransientPredicate_OverridesDefaultDetection()
    {
        var callCount = 0;

        await BulkUpsertRetryPolicy.ExecuteAsync(
            () =>
            {
                callCount++;
                if (callCount < 2) throw new ArgumentException("custom-marker");
                return Task.CompletedTask;
            },
            CancellationToken.None,
            isTransient: ex => ex is ArgumentException,
            baseDelay: TimeSpan.FromMilliseconds(1));

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequestedBeforeAttempt_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(
            () => Task.CompletedTask,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_CancellationDuringBackoffDelay_PropagatesWithoutFurtherRetries()
    {
        using var cts = new CancellationTokenSource();
        var callCount = 0;

        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(
            () =>
            {
                callCount++;
                cts.Cancel();
                throw new InvalidOperationException("database is locked");
            },
            cts.Token,
            baseDelay: TimeSpan.FromMilliseconds(50));

        await act.Should().ThrowAsync<OperationCanceledException>();
        callCount.Should().Be(1);
    }

    [Fact]
    public void ExecuteAsync_NullOperation_ThrowsArgumentNullException()
    {
        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(null!, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void ExecuteAsync_MaxAttemptsLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        Func<Task> act = () => BulkUpsertRetryPolicy.ExecuteAsync(
            () => Task.CompletedTask, CancellationToken.None, maxAttempts: 0);

        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
