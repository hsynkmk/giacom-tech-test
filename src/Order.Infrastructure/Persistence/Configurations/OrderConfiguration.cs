using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("order");

        builder.HasIndex(x => x.CustomerId).HasDatabaseName("CustomerId");
        builder.HasIndex(x => x.StatusId).HasDatabaseName("StatusId");

        builder.HasOne(x => x.Status)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_ofk_1");
    }
}
