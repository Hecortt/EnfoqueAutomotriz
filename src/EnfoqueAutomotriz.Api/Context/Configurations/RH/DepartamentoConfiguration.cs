using EnfoqueAutomotriz.Domain.RH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.RH;

public sealed class DepartamentoConfiguration : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> b)
    {
        b.ToTable("departamentos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Descripcion).HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class PuestoConfiguration : IEntityTypeConfiguration<Puesto>
{
    public void Configure(EntityTypeBuilder<Puesto> b)
    {
        b.ToTable("puestos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Descripcion).HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.Departamento)
            .WithMany(d => d.Puestos)
            .HasForeignKey(e => e.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> b)
    {
        b.ToTable("empleados");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
        b.Property(e => e.Nombres).HasMaxLength(150).IsRequired();
        b.Property(e => e.ApellidoPaterno).HasMaxLength(100).IsRequired();
        b.Property(e => e.ApellidoMaterno).HasMaxLength(100);
        b.Property(e => e.Telefono).HasMaxLength(20);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.SalarioDiario).HasPrecision(10, 2);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.Ignore(e => e.NombreCompleto);

        b.HasOne(e => e.Sucursal).WithMany().HasForeignKey(e => e.SucursalId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(e => e.Puesto).WithMany().HasForeignKey(e => e.PuestoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.JefeDirecto)
            .WithMany(e => e.Subordinados)
            .HasForeignKey(e => e.JefeDirectoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.Codigo).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
