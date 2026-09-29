using EnfoqueAutomotriz.Domain.Configuracion;
using EnfoqueAutomotriz.Domain.Ordenes;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnfoqueAutomotriz.Api.Helpers;

/// <summary>
/// Genera el PDF de la Orden de Servicio (ticket de recepción / nota de entrega),
/// con código QR de seguimiento público (sin necesidad de cuenta).
/// Patrón adaptado de los generadores de Cotización/Factura de Obra360 (QuestPDF).
/// </summary>
public class OrdenServicioPdfGenerator : IDocument
{
    private readonly OrdenServicio _orden;
    private readonly ConfiguracionEmpresa? _empresa;
    private readonly byte[]? _logoBytes;
    private readonly string _urlSeguimiento;

    private OrdenServicioPdfGenerator(OrdenServicio orden, ConfiguracionEmpresa? empresa, byte[]? logoBytes, string baseUrlUi)
    {
        _orden = orden;
        _empresa = empresa;
        _logoBytes = logoBytes;
        _urlSeguimiento = $"{baseUrlUi.TrimEnd('/')}/Seguimiento/{orden.TokenSeguimiento}";
    }

    public static byte[] Generate(OrdenServicio orden, ConfiguracionEmpresa? empresa, byte[]? logoBytes, string baseUrlUi)
        => new OrdenServicioPdfGenerator(orden, empresa, logoBytes, baseUrlUi).GeneratePdf();

    public byte[] GeneratePdf() => Document.Create(Compose).GeneratePdf();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.MarginVertical(35);
            page.MarginHorizontal(45);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    if (_logoBytes != null)
                        c.Item().Height(45).Image(_logoBytes);
                    else
                        c.Item().Text(_empresa?.NombreComercial ?? "Taller").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);

                    if (!string.IsNullOrEmpty(_empresa?.RazonSocial)) c.Item().Text(_empresa.RazonSocial).FontSize(8);
                    if (!string.IsNullOrEmpty(_empresa?.Direccion)) c.Item().Text(_empresa.Direccion).FontSize(8);
                    if (!string.IsNullOrEmpty(_empresa?.Telefono)) c.Item().Text($"Tel: {_empresa.Telefono}").FontSize(8);
                });

                row.ConstantItem(190).Column(c =>
                {
                    c.Item().Background(Colors.Blue.Darken3).Padding(6).AlignCenter()
                        .Text("ORDEN DE SERVICIO").FontColor(Colors.White).Bold().FontSize(12);

                    c.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                        AddRow(t, "Folio:", _orden.Folio);
                        AddRow(t, "Fecha:", _orden.FechaRecepcion.ToString("dd/MM/yyyy HH:mm"));
                        AddRow(t, "Estatus:", _orden.Estado.ToString());
                        AddRow(t, "Sucursal:", _orden.Sucursal?.Nombre ?? "-");
                    });
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

            col.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("CLIENTE").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text(_orden.Cliente?.Nombre ?? "-").FontSize(10);
                    if (!string.IsNullOrEmpty(_orden.Cliente?.Telefono)) c.Item().Text(_orden.Cliente.Telefono).FontSize(8);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("VEHÍCULO").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"{_orden.Vehiculo?.Marca} {_orden.Vehiculo?.Modelo} {_orden.Vehiculo?.Anio}").FontSize(10);
                    c.Item().Text($"Placas: {_orden.Vehiculo?.Placas}  ·  Km: {_orden.KilometrajeRecepcion}").FontSize(8);
                });
            });
        });
    }

    private static void AddRow(TableDescriptor t, string label, string value)
    {
        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(label).FontSize(8);
        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(value).FontSize(8).Bold();
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Text("Problema reportado").Bold().FontSize(9);
            col.Item().PaddingBottom(8).Text(_orden.ProblemaReportado).FontSize(9);

            var conceptos = _orden.Conceptos.Where(c => !c.IsDeleted).ToList();
            if (conceptos.Count > 0)
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(1.3f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Descripción").FontColor(Colors.White).FontSize(8).Bold();
                        h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("Cant.").FontColor(Colors.White).FontSize(8).Bold();
                        h.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("P. Unitario").FontColor(Colors.White).FontSize(8).Bold();
                        h.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignRight().Text("Importe").FontColor(Colors.White).FontSize(8).Bold();
                    });

                    var zebra = false;
                    foreach (var c in conceptos)
                    {
                        var bg = zebra ? Colors.Grey.Lighten4 : Colors.White;
                        table.Cell().Background(bg).Padding(4).Text(c.Descripcion).FontSize(8);
                        table.Cell().Background(bg).Padding(4).Text(c.Cantidad.ToString("0.##")).FontSize(8);
                        table.Cell().Background(bg).Padding(4).AlignRight().Text(c.PrecioUnitario.ToString("C2")).FontSize(8);
                        table.Cell().Background(bg).Padding(4).AlignRight().Text(c.Importe.ToString("C2")).FontSize(8);
                        zebra = !zebra;
                    }
                });

                col.Item().PaddingTop(8).AlignRight().Width(220).Column(c =>
                {
                    Total(c, "Subtotal", _orden.Subtotal);
                    if (_orden.Descuento > 0) Total(c, "Descuento", -_orden.Descuento);
                    Total(c, "IVA (16%)", _orden.Iva);
                    c.Item().LineHorizontal(0.5f);
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL").Bold().FontSize(11);
                        r.RelativeItem().AlignRight().Text(_orden.Total.ToString("C2")).Bold().FontSize(11);
                    });
                    if (_orden.Anticipo > 0)
                    {
                        Total(c, "Anticipo", -_orden.Anticipo);
                        Total(c, "Saldo", _orden.Saldo);
                    }
                });
            }

            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Seguimiento en línea").Bold().FontSize(9);
                    c.Item().Text("Escanea el código QR para ver el avance de tu vehículo,\naprobar cotizaciones y recibir notificaciones,\nsin necesidad de registrarte.").FontSize(8);
                    c.Item().PaddingTop(4).Text(_urlSeguimiento).FontSize(7).FontColor(Colors.Blue.Darken2);
                });
                row.ConstantItem(90).Height(90).Image(GenerarQr(_urlSeguimiento));
            });

            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().PaddingTop(20).LineHorizontal(0.5f);
                    c.Item().AlignCenter().Text("Firma del cliente").FontSize(8);
                });
                row.ConstantItem(20);
                row.RelativeItem().Column(c =>
                {
                    c.Item().PaddingTop(20).LineHorizontal(0.5f);
                    c.Item().AlignCenter().Text("Firma del asesor").FontSize(8);
                });
            });
        });
    }

    private static void Total(ColumnDescriptor c, string label, decimal value)
    {
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(9);
            r.RelativeItem().AlignRight().Text(value.ToString("C2")).FontSize(9);
        });
    }

    private static byte[] GenerarQr(string contenido)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(8);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(7);
                    text.Span(" / ").FontSize(7);
                    text.TotalPages().FontSize(7);
                });
            });
        });
    }
}
