using B2BPredictor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2BPredictor.Infrastructure.Persistence.Configurations;

public sealed class PredictionHistoryConfiguration : IEntityTypeConfiguration<PredictionHistory>
{
    public void Configure(EntityTypeBuilder<PredictionHistory> builder)
    {
        builder.ToTable("PredictionHistory", table =>
            table.HasCheckConstraint("CK_PredictionHistory_SuccessProbability", "\"SuccessProbability\" BETWEEN 0 AND 100"));

        builder.HasKey(prediction => prediction.Id);

        builder.Property(prediction => prediction.SuccessProbability)
            .IsRequired();

        builder.Property(prediction => prediction.IsLikelySuccessful)
            .IsRequired();

        builder.Property(prediction => prediction.RiskLevel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(prediction => prediction.ModelVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(prediction => prediction.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(prediction => prediction.OrderId);
        builder.HasIndex(prediction => new { prediction.OrderId, prediction.CreatedAtUtc });

        builder.HasOne(prediction => prediction.Order)
            .WithMany(order => order.Predictions)
            .HasForeignKey(prediction => prediction.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(prediction => prediction.RiskFactors)
            .WithOne(riskFactor => riskFactor.PredictionHistory)
            .HasForeignKey(riskFactor => riskFactor.PredictionHistoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
