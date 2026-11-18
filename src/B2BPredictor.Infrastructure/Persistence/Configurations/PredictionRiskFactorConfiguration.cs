using B2BPredictor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2BPredictor.Infrastructure.Persistence.Configurations;

public sealed class PredictionRiskFactorConfiguration : IEntityTypeConfiguration<PredictionRiskFactor>
{
    public void Configure(EntityTypeBuilder<PredictionRiskFactor> builder)
    {
        builder.ToTable("PredictionRiskFactors");

        builder.HasKey(riskFactor => riskFactor.Id);

        builder.Property(riskFactor => riskFactor.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(riskFactor => riskFactor.PredictionHistoryId);

        builder.HasOne(riskFactor => riskFactor.PredictionHistory)
            .WithMany(prediction => prediction.RiskFactors)
            .HasForeignKey(riskFactor => riskFactor.PredictionHistoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
