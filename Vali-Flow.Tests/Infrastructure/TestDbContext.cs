using Microsoft.EntityFrameworkCore;
using Vali_Flow.Tests.Models;

namespace Vali_Flow.Tests.Infrastructure;

public sealed class TestDbContext : DbContext
{
    public DbSet<TestOrder> Orders => Set<TestOrder>();
    public DbSet<TestCustomer> Customers => Set<TestCustomer>();
    public DbSet<TestOrderItem> OrderItems => Set<TestOrderItem>();
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}
