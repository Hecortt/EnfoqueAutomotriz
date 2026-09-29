namespace EnfoqueAutomotriz.UI.Pages.Shared;

public class AyudaModel
{
    /// <summary>Identificador único de la página (para el drawer)</summary>
    public string PaginaId { get; set; } = "";

    /// <summary>Texto que aparece en el header del panel</summary>
    public string Titulo { get; set; } = "¿Cómo usar esta pantalla?";

    /// <summary>Descripción de qué hace la pantalla</summary>
    public string QueHace { get; set; } = "";

    /// <summary>Pasos numerados para usar la pantalla</summary>
    public List<PasoAyuda> Pasos { get; set; } = [];

    /// <summary>Módulos relacionados con los que se conecta</summary>
    public List<ConexionAyuda> Conexiones { get; set; } = [];

    /// <summary>Advertencias importantes</summary>
    public List<string> Alertas { get; set; } = [];
}

public record PasoAyuda(int Numero, string Descripcion);

public record ConexionAyuda(string Nombre, string Url, string Icono, string Motivo);
