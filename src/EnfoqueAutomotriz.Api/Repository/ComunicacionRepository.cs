using EnfoqueAutomotriz.Api.Context;
using EnfoqueAutomotriz.Api.Repository.IRepository;
using EnfoqueAutomotriz.Domain.Comunicacion;
using Microsoft.EntityFrameworkCore;

namespace EnfoqueAutomotriz.Api.Repository;

public class ComunicacionRepository : IComunicacionRepository
{
    private readonly AppDbContext _db;
    public ComunicacionRepository(AppDbContext db) => _db = db;

    public Task<List<PlantillaMensaje>> GetPlantillasAsync(CancellationToken ct = default) =>
        _db.PlantillasMensaje.OrderBy(p => p.Orden).ToListAsync(ct);

    public async Task CreatePlantillaAsync(PlantillaMensaje plantilla, CancellationToken ct = default)
    {
        _db.PlantillasMensaje.Add(plantilla);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<MensajeEnviado>> GetHistorialAsync(Guid? clienteId = null, CancellationToken ct = default)
    {
        var query = _db.MensajesEnviados.AsQueryable();
        if (clienteId.HasValue) query = query.Where(m => m.ClienteId == clienteId.Value);
        return query.OrderByDescending(m => m.Fecha).Take(200).ToListAsync(ct);
    }

    public async Task<MensajeEnviado> EnviarSimuladoAsync(Guid clienteId, Guid? ordenServicioId, Guid plantillaId,
        string destinatario, Dictionary<string, string> placeholders, CancellationToken ct = default)
    {
        var plantilla = await _db.PlantillasMensaje.FirstOrDefaultAsync(p => p.Id == plantillaId, ct);
        var contenido = plantilla?.Cuerpo ?? "";
        foreach (var (key, value) in placeholders)
            contenido = contenido.Replace("{{" + key + "}}", value);

        var mensaje = new MensajeEnviado
        {
            ClienteId = clienteId,
            OrdenServicioId = ordenServicioId,
            PlantillaMensajeId = plantillaId,
            Canal = plantilla?.Canal ?? CanalMensaje.WhatsApp,
            Destinatario = destinatario,
            Contenido = contenido,
            Estado = EstadoMensajeEnviado.Simulado
        };

        _db.MensajesEnviados.Add(mensaje);
        await _db.SaveChangesAsync(ct);
        return mensaje;
    }
}
