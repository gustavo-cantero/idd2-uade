using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace IDD2.Model;

public class UsuarioModel
{
    [BsonId] // Indica que este campo será el "_id" en MongoDB
    [BsonRepresentation(BsonType.ObjectId)] // Permite manejar el ID como un string si es necesario
    public string? id { get; set; }
    public string nombre { get; set; }
    public string nombreApellido { get; set; }
    public string direccion { get; set; }
    public string provincia { get; set; }
    public string localidad { get; set; }
    public string telefono { get; set; }
    public bool? activo { get; set; } = true;
    public DateTime? fechaCreacion { get; set; }
    public DateTime? fechaModificacion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string contraseña { get; set; }
}
