using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Configuracion;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class ConfiguracionEmpresaRepository : IConfiguracionEmpresaRepository
{
    private readonly AppDbContext _db;

    public ConfiguracionEmpresaRepository(AppDbContext db) => _db = db;

    public Task<ConfiguracionEmpresa?> GetAsync(CancellationToken ct = default) =>
        _db.ConfiguracionEmpresa.FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(ConfiguracionEmpresa config, CancellationToken ct = default)
    {
        var existing = await _db.ConfiguracionEmpresa.FirstOrDefaultAsync(ct);
        if (existing == null)
        {
            _db.ConfiguracionEmpresa.Add(config);
        }
        else
        {
            existing.NombreComercial = config.NombreComercial;
            existing.RazonSocial = config.RazonSocial;
            existing.Rfc = config.Rfc;
            existing.LogoUrl = config.LogoUrl;
            existing.Direccion = config.Direccion;
            existing.Telefono = config.Telefono;
            existing.Email = config.Email;
            existing.SitioWeb = config.SitioWeb;
            existing.Banco = config.Banco;
            existing.Clabe = config.Clabe;
            existing.CuentaBancaria = config.CuentaBancaria;
            existing.TitularCuenta = config.TitularCuenta;
            existing.Moneda = config.Moneda;
            existing.LeyendaFactura = config.LeyendaFactura;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
