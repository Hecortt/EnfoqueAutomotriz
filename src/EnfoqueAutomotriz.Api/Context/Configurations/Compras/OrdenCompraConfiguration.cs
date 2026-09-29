using EnfoqueAutomotriz.Domain.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Compras;

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> b)
    {
        b.ToTable("proveedores");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
        b.Property(e => e.Contacto).HasMaxLength(150);
        b.Property(e => e.Telefono).HasMaxLength(20);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Direccion).HasMaxLength(300);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class OrdenCompraConfiguration : IEntityTypeConfiguration<OrdenCompra>
{
    public void Configure(EntityTypeBuilder<OrdenCompra> b)
    {
        b.ToTable("ordenes_compra");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Folio).HasMaxLength(20).IsRequired();
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Total).HasPrecision(12, 2);
        b.Property(e => e.Observaciones).HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.Proveedor).WithMany().HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.Folio).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class OrdenCompraDetalleConfiguration : IEntityTypeConfiguration<OrdenCompraDetalle>
{
    public void Configure(EntityTypeBuilder<OrdenCompraDetalle> b)
    {
        b.ToTable("ordenes_compra_detalle");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.CostoUnitario).HasPrecision(12, 2);

        b.HasOne(e => e.Refaccion).WithMany().HasForeignKey(e => e.RefaccionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.OrdenCompraId);
    }
}
