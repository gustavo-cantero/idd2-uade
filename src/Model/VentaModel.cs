using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IDD2.Model;

public class VentaModel
{
    [BsonId] // Indica que este campo será el "_id" en MongoDB
    [BsonRepresentation(BsonType.ObjectId)] // Permite manejar el ID como un string si es necesario
    public string id { get; set; }
    public string usuarioId { get; set; }
    public double total { get; set; }
    public DateTime fecha { get; set; }
    public string facturaId { get; set; }
    public PagoModel pago { get; set; }
    public IEnumerable<ProductoEnCarritoModel> productos { get; set; }
}