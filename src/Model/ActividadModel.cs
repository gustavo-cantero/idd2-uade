using System.Text.Json.Serialization;

namespace IDD2.Model;

public class ActividadModel
{
    public string producto { get; set; }

    public string id { get; set; }

    public DateTime fecha { get; set; }

    public string accion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? cantidad { get; set; }
}
