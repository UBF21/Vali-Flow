namespace Vali_Flow.Benchmarks.Models;

internal static class BenchmarkDataFactory
{
    private static readonly string[] Statuses = ["Pending", "Completed", "Cancelled"];

    public static List<BenchmarkOrder> Generate(int n)
    {
        var baseDate = new DateTime(2024, 1, 1);
        return Enumerable.Range(1, n)
            .Select(i => new BenchmarkOrder
            {
                Id        = i,
                Amount    = i * 1.5m,
                IsActive  = i % 2 == 0,
                Name      = $"Order-{i}",
                Status    = Statuses[i % 3],
                Quantity  = i % 200,
                CreatedAt = baseDate.AddDays(i % 365)
            })
            .ToList();
    }
}
