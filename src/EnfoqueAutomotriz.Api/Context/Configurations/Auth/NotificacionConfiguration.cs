using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Auth;

public sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> b)
    {
        b.ToTable("notificaciones");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Titulo).HasMaxLength(200).IsRequired();
        b.Property(e => e.Mensaje).HasMaxLength(500).IsRequired();
        b.Property(e => e.Url).HasMaxLength(300);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("now() AT TIME ZONE 'UTC'");

        b.HasOne(e => e.Usuario)
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(e => new { e.UsuarioId, e.Leida });
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
