using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class PermisoRolRepository : IPermisoRolRepository
{
    private readonly AppDbContext _db;

    public PermisoRolRepository(AppDbContext db) => _db = db;

    public Task<List<PermisoRol>> GetByRolAsync(int rol, CancellationToken ct = default) =>
        _db.PermisosRol.Where(p => p.Rol == rol).ToListAsync(ct);

    public Task<List<PermisoRol>> GetAllAsync(CancellationToken ct = default) =>
        _db.PermisosRol.OrderBy(p => p.Rol).ThenBy(p => p.Modulo).ToListAsync(ct);

    public async Task UpdateAsync(ActualizarPermisoRolDto dto, CancellationToken ct = default)
    {
        var permiso = await _db.PermisosRol.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        if (permiso == null) return;

        permiso.PuedeVer = dto.PuedeVer;
        permiso.PuedeCrear = dto.PuedeCrear;
        permiso.PuedeEditar = dto.PuedeEditar;
        permiso.PuedeEliminar = dto.PuedeEliminar;
        permiso.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
