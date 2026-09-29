using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Auth;

public sealed class PermisoRolConfiguration : IEntityTypeConfiguration<PermisoRol>
{
    public void Configure(EntityTypeBuilder<PermisoRol> b)
    {
        b.ToTable("permiso_roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.Rol).IsRequired();
        b.Property(x => x.Modulo).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.Property(x => x.PuedeVer).HasDefaultValue(false);
        b.Property(x => x.PuedeCrear).HasDefaultValue(false);
        b.Property(x => x.PuedeEditar).HasDefaultValue(false);
        b.Property(x => x.PuedeEliminar).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("now() AT TIME ZONE 'UTC'");

        b.HasIndex(x => x.Rol);
        b.HasIndex(x => new { x.Rol, x.Modulo }).IsUnique();

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
