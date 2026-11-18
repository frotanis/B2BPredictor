using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace B2BPredictor.Infrastructure.Persistence;

public sealed class B2BPredictorDbContextFactory : IDesignTimeDbContextFactory<B2BPredictorDbContext>
{
    public B2BPredictorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<B2BPredictorDbContext>();
        optionsBuilder.UseSqlite("Data Source=b2bpredictor.db");
        return new B2BPredictorDbContext(optionsBuilder.Options);
    }
}
