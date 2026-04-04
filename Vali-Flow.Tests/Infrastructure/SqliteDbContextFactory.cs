using Microsoft.EntityFrameworkCore;

namespace Vali_Flow.Tests.Infrastructure;

public static class SqliteDbContextFactory
{
    public static TestDbContext Create()
    {
        var opts = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var ctx = new TestDbContext(opts);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
