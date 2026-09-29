using EnfoqueAutomotriz.Domain.Almacen;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Almacen;

public sealed class RefaccionConfiguration : IEntityTypeConfiguration<Refaccion>
{
    public void Configure(EntityTypeBuilder<Refaccion> b)
    {
        b.ToTable("refacciones");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
        b.Property(e => e.Sku).HasMaxLength(50).IsRequired();
        b.Property(e => e.Categoria).HasMaxLength(100);
        b.Property(e => e.Unidad).HasMaxLength(10);
        b.Property(e => e.Costo).HasPrecision(12, 2);
        b.Property(e => e.PrecioVenta).HasPrecision(12, 2);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.Ignore(e => e.Nivel);

        b.HasIndex(e => e.Sku).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> b)
    {
        b.ToTable("movimientos_inventario");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Motivo).HasMaxLength(300);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.Refaccion).WithMany().HasForeignKey(e => e.RefaccionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.RefaccionId);
    }
}

public sealed class SolicitudAlmacenConfiguration : IEntityTypeConfiguration<SolicitudAlmacen>
{
    public void Configure(EntityTypeBuilder<SolicitudAlmacen> b)
    {
        b.ToTable("solicitudes_almacen");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Observaciones).HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio).WithMany().HasForeignKey(e => e.OrdenServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Sucursal).WithMany().HasForeignKey(e => e.SucursalId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.OrdenServicioId);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class SolicitudAlmacenDetalleConfiguration : IEntityTypeConfiguration<SolicitudAlmacenDetalle>
{
    public void Configure(EntityTypeBuilder<SolicitudAlmacenDetalle> b)
    {
        b.ToTable("solicitudes_almacen_detalle");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        b.HasOne(e => e.Refaccion).WithMany().HasForeignKey(e => e.RefaccionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => e.SolicitudAlmacenId);
    }
}
