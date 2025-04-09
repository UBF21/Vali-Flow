using Microsoft.EntityFrameworkCore;
using vali_flow_test.Models;

namespace vali_flow_test.DbConext;

public class AppDbContext : DbContext
{
    public DbSet<Modulo> Modulos { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}