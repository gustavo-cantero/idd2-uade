using System.Text.Json.Serialization;

namespace IDD2.Model;

public class ProductoHistorialModel : ProductoModel
{
    public string productId { get; set; }
    public DateTime fechaVersion { get; set; }
    [JsonIgnore] //Para que no se muestren las opiniones vacías
    public override IEnumerable<OpinionModel>? opiniones { get; set; }
}
