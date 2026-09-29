using EnfoqueAutomotriz.Domain.Promociones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Promociones;

public sealed class PromocionConfiguration : IEntityTypeConfiguration<Promocion>
{
    public void Configure(EntityTypeBuilder<Promocion> b)
    {
        b.ToTable("promociones");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Descripcion).HasMaxLength(500);
        b.Property(e => e.TipoDescuento).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.AplicaA).HasConversion<string>().HasMaxLength(30);
        b.Property(e => e.Valor).HasPrecision(10, 2);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.Ignore(e => e.Vigente);

        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
