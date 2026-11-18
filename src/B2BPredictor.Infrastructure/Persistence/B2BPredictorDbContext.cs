using B2BPredictor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.Infrastructure.Persistence;

public class B2BPredictorDbContext : DbContext
{
    public B2BPredictorDbContext(DbContextOptions<B2BPredictorDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<PredictionHistory> Predictions => Set<PredictionHistory>();

    public DbSet<PredictionRiskFactor> PredictionRiskFactors => Set<PredictionRiskFactor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(B2BPredictorDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
