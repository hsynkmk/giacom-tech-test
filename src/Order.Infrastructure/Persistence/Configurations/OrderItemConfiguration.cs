using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_item");

        builder.HasIndex(x => x.OrderId).HasDatabaseName("OrderId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ProductId");
        builder.HasIndex(x => x.ServiceId).HasDatabaseName("ServiceId");

        builder.Property(x => x.Quantity).HasColumnType("int(11)");

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_item_oifk_1");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_product_oifk_1");

        builder.HasOne(x => x.Service)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_service_oifk_1");
    }
}
