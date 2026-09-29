using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.Catalogs;

/// <summary>
/// Catálogo genérico multi-propósito. Permite definir listas de valores configurables
/// por tenant sin necesidad de crear una tabla nueva por cada catálogo
/// (ej. especialidades del taller, tipos de servicio, marcas, estatus de orden, etc.).
/// El campo <see cref="Tipo"/> actúa como discriminador (ej. "especialidades", "tipos_servicio").
/// </summary>
public class Catalogo : CatalogEntity
{
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Color { get; set; }
    public string? Icono { get; set; }
}
