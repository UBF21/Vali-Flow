using Xunit;

// Several test classes in this project assert on System.Diagnostics.Activity captured via a
// process-wide ActivityListener on ValiFlowDiagnostics.Source (see e.g. SqlDeleteBuilderTests'
// AttachScopedListener/GetOwn helpers). Parenting each test's activities under a private root and
// filtering by ParentId reduces -- but does not eliminate -- cross-test collisions under xUnit's
// default test-class-level parallelism: it was still observed intermittently failing with "Sequence
// contains no matching element" across the 6 SQL builder test classes. Disabling parallelization for
// this assembly removes the race at its source; the full suite still runs in well under a second.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
