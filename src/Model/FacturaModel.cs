using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IDD2.Model;

public class FacturaModel
{
    [BsonId] // Indica que este campo será el "_id" en MongoDB
    [BsonRepresentation(BsonType.ObjectId)] // Permite manejar el ID como un string si es necesario
    public string id { get; set; }
    public VentaModel venta { get; set; }
    public int sucursal { get; set; }
    public long numero { get; set; }
    public DateTime fecha { get; set; }
    public string cae { get; set; }
    internal DateTime vtoCae;
    public PagoModel pago { get; set; }
}