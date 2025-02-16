using IDD2.Data;
using IDD2.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace IDD2.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController(ConfigurationModel config) : ControllerBase
{
    [HttpPost("list")]
    public IActionResult ObtenerProductosPaginados([FromBody] FiltroProductos filtro)
    {
        if (filtro == null)
            return BadRequest("Los filtros son obligatorios.");
        return Ok(Product.List(config, filtro));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var res = await Product.Get(config, id);
        if (res == null)
            return NotFound("No se encontr� el producto");
        return Ok(res);
    }

    [HttpPost()]
    public async Task<IActionResult> Insert([FromBody] ProductoModel product)
    {
        try
        {
            await Product.Insert(config, product);
            return Ok(product);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict("Ya existe otro producto con ese nombre");
        }
    }

    [HttpPut()]
    public async Task<IActionResult> Update([FromBody] ProductoModel product)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(product.id))
                return BadRequest("El identificador del producto es requerido");
            var res = await Product.Update(config, product);
            if (res == null)
                return NotFound("No se encontr� el producto");
            return Ok(res);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return Conflict("Ya existe otro producto con ese nombre");
        }
    }

    [HttpPatch("{id}/price/{price:double}")]
    public async Task<IActionResult> SetPrice(string id, double price)
    {
        if (await Product.SetPrice(config, id, price))
            return Ok();
        return NotFound("No se encontr� el producto");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (await Product.Delete(config, id))
            return Ok();
        return NotFound("No se encontr� el producto");
    }

    [HttpPost("{id}/opinion")]
    public async Task<IActionResult> InsertOpinion(string id, [FromBody] OpinionModel opinion)
    {
        if (opinion.puntaje < 1 || opinion.puntaje > 5)
            return BadRequest("El puntaje debe ser un n�mero entre 1 y 5");
        if (await Product.InsertOpinion(config, id, opinion))
            return Ok();
        return NotFound("No se encontr� el producto");
    }
}
