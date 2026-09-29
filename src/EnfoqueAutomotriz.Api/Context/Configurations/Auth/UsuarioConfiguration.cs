using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Auth;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Email).HasMaxLength(200).IsRequired();
        b.Property(e => e.NombreCompleto).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        b.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(e => e.Rol);
        b.Property(e => e.Activo);
        b.Property(e => e.Bloqueado);
        b.Property(e => e.IntentosFallidos);
        b.Property(e => e.UltimoAcceso);
        b.Property(e => e.FechaBloqueo).HasColumnName("bloqueado_hasta");
        b.Property(e => e.FotoUrl);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasIndex(e => e.Email).IsUnique();
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
