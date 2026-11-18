using B2BPredictor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2BPredictor.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Amount", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_Orders_PaymentTermsDays", "\"PaymentTermsDays\" BETWEEN 0 AND 365");
            table.HasCheckConstraint("CK_Orders_ItemsCount", "\"ItemsCount\" BETWEEN 1 AND 10000");
            table.HasCheckConstraint("CK_Orders_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected', 'Completed')");
            table.HasCheckConstraint("CK_Orders_SuccessProbability", "\"SuccessProbability\" IS NULL OR (\"SuccessProbability\" >= 0 AND \"SuccessProbability\" <= 100)");
        });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Amount)
            .HasConversion(MoneyValueConverter.Instance)
            .IsRequired();

        builder.Property(order => order.PaymentTermsDays)
            .IsRequired();

        builder.Property(order => order.ItemsCount)
            .IsRequired();

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(order => order.Notes)
            .HasMaxLength(500);

        builder.Property(order => order.RiskLevel)
            .HasMaxLength(50);

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(order => order.ClientId);
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => new { order.ClientId, order.Status });
        builder.HasIndex(order => new { order.Status, order.CreatedAtUtc });

        builder.HasOne(order => order.Client)
            .WithMany(client => client.Orders)
            .HasForeignKey(order => order.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(order => order.Predictions)
            .WithOne(prediction => prediction.Order)
            .HasForeignKey(prediction => prediction.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
