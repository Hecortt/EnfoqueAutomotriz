using EnfoqueAutomotriz.Domain.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnfoqueAutomotriz.Api.Context.Configurations.Clientes;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
        b.Property(e => e.Telefono).HasMaxLength(20);
        b.Property(e => e.Email).HasMaxLength(200);
        b.Property(e => e.Rfc).HasMaxLength(13);
        b.Property(e => e.Direccion).HasMaxLength(300);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasIndex(e => e.Telefono);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}

public sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.ToTable("vehiculos");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        b.Property(e => e.Marca).HasMaxLength(80).IsRequired();
        b.Property(e => e.Modelo).HasMaxLength(80).IsRequired();
        b.Property(e => e.Placas).HasMaxLength(20).IsRequired();
        b.Property(e => e.Color).HasMaxLength(40);
        b.Property(e => e.Vin).HasMaxLength(30);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        b.HasOne(e => e.Cliente)
            .WithMany(c => c.Vehiculos)
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(e => e.Placas);
        b.HasQueryFilter(e => !e.IsDeleted);
    }
}
