using EscrowApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EscrowApp.Data.Configurations;

public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ClientEmail)
            .IsRequired();

        builder.Property(e => e.ConsultantEmail)
            .IsRequired();

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasColumnType("numeric(18,4)");

        builder.Property(e => e.ServiceDescription)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}
