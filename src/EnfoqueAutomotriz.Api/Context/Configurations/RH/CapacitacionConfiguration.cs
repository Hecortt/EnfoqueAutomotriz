using EnfoqueAutomotriz.Domain.RH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.RH;

public sealed class CapacitacionConfiguration : IEntityTypeConfiguration<Capacitacion>
{
    public void Configure(EntityTypeBuilder<Capacitacion> b)
    {
        b.ToTable("capacitaciones");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Titulo).HasMaxLength(200).IsRequired();
        b.Property(e => e.Descripcion).HasMaxLength(1000);
        b.Property(e => e.Categoria).HasMaxLength(100);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class EmpleadoCapacitacionConfiguration : IEntityTypeConfiguration<EmpleadoCapacitacion>
{
    public void Configure(EntityTypeBuilder<EmpleadoCapacitacion> b)
    {
        b.ToTable("empleados_capacitaciones");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.CalificacionFinal).HasPrecision(5, 2);
        b.Property(e => e.FechaAsignacion).HasDefaultValueSql("NOW()");
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.Empleado).WithMany().HasForeignKey(e => e.EmpleadoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Capacitacion)
            .WithMany(c => c.Asignaciones)
            .HasForeignKey(e => e.CapacitacionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => e.EmpleadoId);
        b.HasIndex(e => e.CapacitacionId);
        b.HasIndex(e => new { e.EmpleadoId, e.CapacitacionId }).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
