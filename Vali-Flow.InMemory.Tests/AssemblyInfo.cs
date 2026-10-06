using Xunit;

// ValiFlowInMemoryDiagnosticsTests asserts on System.Diagnostics.Activity captured via a process-wide
// ActivityListener on ValiFlowDiagnostics.Source, parenting each test's activities under a private
// root and filtering by ParentId. That reduces -- but does not fully eliminate -- cross-test
// collisions under xUnit's default test-class-level parallelism: the identical pattern was observed
// intermittently failing with "Sequence contains no matching element" in Vali-Flow.Sql.Tests (which
// has more Activity-asserting test classes competing concurrently). Disabling parallelization for
// this assembly removes that race at its source.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
