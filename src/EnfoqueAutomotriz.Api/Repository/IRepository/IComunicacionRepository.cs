using EnfoqueAutomotriz.Domain.Comunicacion;

namespace EnfoqueAutomotriz.Api.Repository.IRepository;

public interface IComunicacionRepository
{
    Task<List<PlantillaMensaje>> GetPlantillasAsync(CancellationToken ct = default);
    Task CreatePlantillaAsync(PlantillaMensaje plantilla, CancellationToken ct = default);

    Task<List<MensajeEnviado>> GetHistorialAsync(Guid? clienteId = null, CancellationToken ct = default);
    Task<MensajeEnviado> EnviarSimuladoAsync(Guid clienteId, Guid? ordenServicioId, Guid plantillaId, string destinatario, Dictionary<string, string> placeholders, CancellationToken ct = default);
}
