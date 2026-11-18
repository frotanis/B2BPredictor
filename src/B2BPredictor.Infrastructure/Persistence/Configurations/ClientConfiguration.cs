using B2BPredictor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2BPredictor.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients", table =>
            table.HasCheckConstraint("CK_Clients_CreditRating", "\"CreditRating\" BETWEEN 1 AND 100"));

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(client => client.Name);

        builder.Property(client => client.Industry)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(client => client.ContactEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(client => client.ContactEmail)
            .IsUnique();

        builder.Property(client => client.CreditRating)
            .IsRequired();

        builder.Property(client => client.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(client => client.Orders)
            .WithOne(order => order.Client)
            .HasForeignKey(order => order.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
