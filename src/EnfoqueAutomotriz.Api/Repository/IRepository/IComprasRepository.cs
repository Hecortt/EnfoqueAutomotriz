using EnfoqueAutomotriz.Domain.Compras;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IComprasRepository
{
    Task<List<Proveedor>> GetProveedoresAsync(CancellationToken ct = default);
    Task<Proveedor?> GetProveedorByIdAsync(Guid id, CancellationToken ct = default);
    Task CreateProveedorAsync(Proveedor proveedor, CancellationToken ct = default);
    Task UpdateProveedorAsync(Proveedor proveedor, CancellationToken ct = default);

    Task<List<OrdenCompra>> GetOrdenesAsync(EstadoOrdenCompra? estado = null, CancellationToken ct = default);
    Task<OrdenCompra?> GetOrdenByIdAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerarFolioAsync(CancellationToken ct = default);
    Task CreateOrdenAsync(OrdenCompra orden, CancellationToken ct = default);
    Task RecibirOrdenAsync(Guid ordenId, CancellationToken ct = default);
}
