using EnfoqueAutomotriz.Domain.Sucursales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Sucursales;

public sealed class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> b)
    {
        b.ToTable("sucursales");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Direccion).HasMaxLength(300);
        b.Property(e => e.Telefono).HasMaxLength(20);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
