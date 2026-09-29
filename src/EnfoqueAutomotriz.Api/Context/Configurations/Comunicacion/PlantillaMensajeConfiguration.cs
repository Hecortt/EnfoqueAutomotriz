using EnfoqueAutomotriz.Domain.Comunicacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Comunicacion;

public sealed class PlantillaMensajeConfiguration : IEntityTypeConfiguration<PlantillaMensaje>
{
    public void Configure(EntityTypeBuilder<PlantillaMensaje> b)
    {
        b.ToTable("plantillas_mensaje");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Asunto).HasMaxLength(200);
        b.Property(e => e.Cuerpo).HasMaxLength(1000).IsRequired();
        b.Property(e => e.Canal).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class MensajeEnviadoConfiguration : IEntityTypeConfiguration<MensajeEnviado>
{
    public void Configure(EntityTypeBuilder<MensajeEnviado> b)
    {
        b.ToTable("mensajes_enviados");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Canal).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Destinatario).HasMaxLength(200).IsRequired();
        b.Property(e => e.Contenido).HasMaxLength(1000).IsRequired();
        b.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.Fecha).HasDefaultValueSql("NOW()");

        b.HasIndex(e => e.ClienteId);
    }
}
