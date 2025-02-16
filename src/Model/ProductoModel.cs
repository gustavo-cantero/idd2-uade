using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IDD2.Model;

public class ProductoModel
{
    [BsonId] // Indica que este campo será el "_id" en MongoDB
    [BsonRepresentation(BsonType.ObjectId)] // Permite manejar el ID como un string si es necesario
    public string? id { get; set; }
    public string nombre { get; set; }
    public double precio { get; set; }
    public int stock { get; set; }
    public string descripcion { get; set; }
    public string categoria { get; set; }
    public string codigoDeBarras { get; set; }
    public DateTime? fechaCreacion { get; set; }
    public DateTime? fechaModificacion { get; set; }
    public double puntaje { get; set; }
    public IEnumerable<OpinionModel>? opiniones { get; set; } = [];
    public string urlImagen { get; set; }
}
