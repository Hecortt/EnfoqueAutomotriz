using EnfoqueAutomotriz.Domain.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Configuracion;

public sealed class ConfiguracionEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracionEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEmpresa> b)
    {
        b.ToTable("configuracion_empresa");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.NombreComercial).HasMaxLength(200).IsRequired();
        b.Property(e => e.RazonSocial).HasMaxLength(300);
        b.Property(e => e.Rfc).HasMaxLength(13);
        b.Property(e => e.Moneda).HasMaxLength(10);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
