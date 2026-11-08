using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.ToTable("order_status");

        builder.HasIndex(x => x.Name).HasDatabaseName("Status");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);
    }
}
