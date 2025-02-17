namespace IDD2.Model;

public class FiltroVentasModel : PaginadoModel
{
    public double? totalMin { get; set; }
    public double? totalMax { get; set; }
    public DateTime? fechaDesde { get; set; }
    public DateTime? fechaHasta { get; set; }
    public bool? pagada { get; set; }
    public string ordenPor { get; set; } = "fecha"; // Campo por el que ordenar, por defecto "fecha"
    public string ordenDireccion { get; set; } = "desc"; // Dirección de orden: "asc" para ascendente, "desc" para descendente
}
