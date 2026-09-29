using EnfoqueAutomotriz.Domain.Shared;

namespace EnfoqueAutomotriz.Domain.RH;

public class Departamento : CatalogEntity
{
    public string? Descripcion { get; set; }
    public ICollection<Puesto> Puestos { get; set; } = [];
}

public class Puesto : CatalogEntity
{
    public string? Descripcion { get; set; }
    public Guid DepartamentoId { get; set; }
    public int Nivel { get; set; } = 1; // 1 = más alto en el organigrama

    public Departamento? Departamento { get; set; }
}

/// <summary>Nodo del árbol del organigrama, construido en memoria a partir de Empleado.JefeDirectoId.</summary>
public class OrganigramaNodo
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Puesto { get; set; }
    public string? Departamento { get; set; }
    public string? FotoUrl { get; set; }
    public List<OrganigramaNodo> Subordinados { get; set; } = [];
}
