using EnfoqueAutomotriz.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Catalogs;

public sealed class CatalogoConfiguration : IEntityTypeConfiguration<Catalogo>
{
    public void Configure(EntityTypeBuilder<Catalogo> b)
    {
        b.ToTable("catalogos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Tipo).HasMaxLength(100).IsRequired();
        b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
        b.Property(e => e.Descripcion).HasMaxLength(500);
        b.Property(e => e.Color).HasMaxLength(20);
        b.Property(e => e.Icono).HasMaxLength(50);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasIndex(e => new { e.Tipo, e.Nombre });
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
