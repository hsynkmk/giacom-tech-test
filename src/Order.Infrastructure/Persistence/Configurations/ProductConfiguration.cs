using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("order_product");

        builder.HasIndex(x => x.ServiceId).HasDatabaseName("order_service_opfk_1");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(x => x.UnitCost).HasColumnType("decimal(4,2)");
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(4,2)");

        builder.HasOne(x => x.Service)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_service_opfk_1");
    }
}
