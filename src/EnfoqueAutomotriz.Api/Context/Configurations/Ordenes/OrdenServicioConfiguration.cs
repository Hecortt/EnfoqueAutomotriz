using EnfoqueAutomotriz.Domain.Ordenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Ordenes;

public sealed class OrdenServicioConfiguration : IEntityTypeConfiguration<OrdenServicio>
{
    public void Configure(EntityTypeBuilder<OrdenServicio> b)
    {
        b.ToTable("ordenes_servicio");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Folio).HasMaxLength(20).IsRequired();
        b.Property(e => e.ProblemaReportado).HasMaxLength(1000).IsRequired();
        b.Property(e => e.NivelGasolina).HasMaxLength(10);
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.EstadoCotizacion).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Subtotal).HasPrecision(12, 2);
        b.Property(e => e.Descuento).HasPrecision(12, 2);
        b.Property(e => e.Iva).HasPrecision(12, 2);
        b.Property(e => e.Total).HasPrecision(12, 2);
        b.Property(e => e.Anticipo).HasPrecision(12, 2);
        b.Property(e => e.Saldo).HasPrecision(12, 2);
        b.Property(e => e.CostoRefacciones).HasPrecision(12, 2);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.Property(e => e.TokenSeguimiento).HasDefaultValueSql("gen_random_uuid()");
        b.Ignore(e => e.EntregadoATiempo);

        b.HasOne(e => e.Sucursal).WithMany().HasForeignKey(e => e.SucursalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Cliente).WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Vehiculo).WithMany().HasForeignKey(e => e.VehiculoId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.Folio).IsUnique();
        b.HasIndex(e => e.TokenSeguimiento).IsUnique();
        b.HasIndex(e => e.Estado);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class OrdenServicioConceptoConfiguration : IEntityTypeConfiguration<OrdenServicioConcepto>
{
    public void Configure(EntityTypeBuilder<OrdenServicioConcepto> b)
    {
        b.ToTable("ordenes_servicio_conceptos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Descripcion).HasMaxLength(300).IsRequired();
        b.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Cantidad).HasPrecision(10, 2);
        b.Property(e => e.PrecioUnitario).HasPrecision(12, 2);
        b.Property(e => e.CostoUnitario).HasPrecision(12, 2);
        b.Property(e => e.Importe).HasPrecision(12, 2);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        // EspecialidadId referencia al catálogo genérico (tipo "especialidades"); sin FK estricta
        // en BD para mantener el mismo patrón flexible que PromocionId en OrdenServicio.

        b.HasOne(e => e.OrdenServicio)
            .WithMany(o => o.Conceptos)
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class OrdenServicioFotoConfiguration : IEntityTypeConfiguration<OrdenServicioFoto>
{
    public void Configure(EntityTypeBuilder<OrdenServicioFoto> b)
    {
        b.ToTable("ordenes_servicio_fotos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Url).HasMaxLength(500).IsRequired();
        b.Property(e => e.Nota).HasMaxLength(300);
        b.Property(e => e.EstadoAlSubir).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio)
            .WithMany(o => o.Fotos)
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class OrdenServicioEstatusHistorialConfiguration : IEntityTypeConfiguration<OrdenServicioEstatusHistorial>
{
    public void Configure(EntityTypeBuilder<OrdenServicioEstatusHistorial> b)
    {
        b.ToTable("ordenes_servicio_historial");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.Comentario).HasMaxLength(500);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio)
            .WithMany(o => o.Historial)
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EncuestaSatisfaccionConfiguration : IEntityTypeConfiguration<EncuestaSatisfaccion>
{
    public void Configure(EntityTypeBuilder<EncuestaSatisfaccion> b)
    {
        b.ToTable("encuestas_satisfaccion");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Comentario).HasMaxLength(500);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio)
            .WithMany()
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OrdenServicioId).IsUnique();
    }
}

public sealed class OrdenServicioMarcaDanoConfiguration : IEntityTypeConfiguration<OrdenServicioMarcaDano>
{
    public void Configure(EntityTypeBuilder<OrdenServicioMarcaDano> b)
    {
        b.ToTable("ordenes_servicio_danos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.X).HasPrecision(5, 2);
        b.Property(e => e.Y).HasPrecision(5, 2);
        b.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Descripcion).HasMaxLength(200);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio)
            .WithMany(o => o.MarcasDano)
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OrdenServicioId);
    }
}

public sealed class OrdenServicioMecanicoConfiguration : IEntityTypeConfiguration<OrdenServicioMecanico>
{
    public void Configure(EntityTypeBuilder<OrdenServicioMecanico> b)
    {
        b.ToTable("ordenes_servicio_mecanicos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.OrdenServicio)
            .WithMany(o => o.Mecanicos)
            .HasForeignKey(e => e.OrdenServicioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.OrdenServicioId);
        b.HasIndex(e => e.EmpleadoId);
        b.HasIndex(e => new { e.OrdenServicioId, e.EmpleadoId }).IsUnique();
    }
}
