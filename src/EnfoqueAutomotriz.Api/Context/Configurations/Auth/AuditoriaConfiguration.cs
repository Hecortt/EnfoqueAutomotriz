using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Auth;

public sealed class AccesoLogConfiguration : IEntityTypeConfiguration<AccesoLog>
{
    public void Configure(EntityTypeBuilder<AccesoLog> b)
    {
        b.ToTable("accesos_log");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Accion).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.IP).HasColumnName("ip").HasMaxLength(45);
        b.Property(e => e.UserAgent).HasMaxLength(500);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");
        b.Property(e => e.Exitoso);
        b.Ignore(e => e.Detalles);
        b.Ignore(e => e.Usuario);

        b.HasIndex(e => e.UsuarioId);
        b.HasIndex(e => e.Fecha);
    }
}

public sealed class AuditoriaGeneralConfiguration : IEntityTypeConfiguration<AuditoriaGeneral>
{
    public void Configure(EntityTypeBuilder<AuditoriaGeneral> b)
    {
        b.ToTable("auditoria_general");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.UsuarioEmail).HasMaxLength(200);
        b.Property(e => e.Entidad).HasMaxLength(100).IsRequired();
        b.Property(e => e.EntidadId).HasMaxLength(100);
        b.Property(e => e.Accion).HasConversion<string>().HasMaxLength(50);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");
        b.Property(e => e.IP).HasColumnName("ip").HasMaxLength(45);
        b.Property(e => e.Modulo).HasMaxLength(100);
        b.Property(e => e.ValoresAnteriores).HasColumnName("valores_previos");
        b.Property(e => e.ValoresNuevos);

        b.Ignore(e => e.Usuario);

        b.HasIndex(e => e.UsuarioId);
        b.HasIndex(e => e.Entidad);
        b.HasIndex(e => e.Fecha);
    }
}
