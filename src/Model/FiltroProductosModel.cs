namespace IDD2.Model;

public class FiltroProductosModel : PaginadoModel
{
    public string? nombre { get; set; }
    public double? precioMin { get; set; }
    public double? precioMax { get; set; }
    public string? codigoDeBarras { get; set; }
    public string? categoria { get; set; }
    public double? puntajeMin { get; set; }
    public string ordenPor { get; set; } = "nombre"; // Campo por el que ordenar, por defecto "nombreApellido"
    public string ordenDireccion { get; set; } = "asc"; // Dirección de orden: "asc" para ascendente, "desc" para descendente
}
