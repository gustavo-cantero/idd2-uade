namespace IDD2.Model;

public class FiltroProductos
{
    public string? nombre { get; set; }
    public double? precioMin { get; set; }
    public double? precioMax { get; set; }
    public string? codigoDeBarras { get; set; }
    public string? categoria { get; set; }
    public double? puntajeMin { get; set; }
    public int pagina { get; set; } = 1; // Página por defecto
    public int elementosPorPagina { get; set; } = 10; // Elementos por página por defecto
    public string ordenPor { get; set; } = "nombre"; // Campo por el que ordenar, por defecto "nombre"
    public string ordenDireccion { get; set; } = "asc"; // Dirección de orden: "asc" para ascendente, "desc" para descendente
}
