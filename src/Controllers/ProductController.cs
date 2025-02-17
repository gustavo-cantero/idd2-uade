using IDD2.Data;
using IDD2.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace IDD2.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController(ConfigurationModel config) : ControllerBase
{
    /// <summary>
    /// Devuelve la lista de productos
    /// </summary>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista paginada de productos</returns>
    [HttpPost("list")]
    public IActionResult List([FromBody] FiltroProductosModel filter)
    {
        if (filter == null)
            return BadRequest("Los filtros son obligatorios");
        return Ok(Product.List(config, filter));
    }

    /// <summary>
    /// Devuelve un producto y registra la visita del usuario
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Detalle del producto</returns>
    [HttpGet("{id}/{userId}")]
    public async Task<IActionResult> Get(string id, string userId)
    {
        var res = await Product.Get(config, id);
        if (res == null)
            return NotFound("No se encontró el producto");
        await Data.User.CreateRelation(config, userId, id, null); //Registro la visita del usuario
        return Ok(res);
    }

    /// <summary>
    /// Inserta un producto
    /// </summary>
    /// <param name="product">Datos del producto</param>
    /// <returns>Detalle del producto insertado</returns>
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

    /// <summary>
    /// Actualiza un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    /// <param name="product">Datos del producto</param>
    /// <returns>Detalle del producto insertado</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductoModel product)
    {
        try
        {
            product.id = id;
            var res = await Product.Update(config, product);
            if (res == null)
                return NotFound("No se encontró el producto");
            return Ok(res);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return Conflict("Ya existe otro producto con ese nombre");
        }
    }

    /// <summary>
    /// Actualiza el precio de un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    /// <param name="price">Nuevo precio</param>
    [HttpPatch("{id}/price/{price:double}")]
    public async Task<IActionResult> SetPrice(string id, double price)
    {
        if (await Product.SetPrice(config, id, price))
            return Ok();
        return NotFound("No se encontró el producto");
    }

    /// <summary>
    /// Actualiza el stock de un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    /// <param name="stock">Nuevo stock</param>
    [HttpPatch("{id}/stock/{stock:int}")]
    public async Task<IActionResult> SetStock(string id, int stock)
    {
        if (await Product.SetStock(config, id, stock))
            return Ok();
        return NotFound("No se encontró el producto");
    }

    /// <summary>
    /// Elimina un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (await Product.Delete(config, id))
            return Ok();
        return NotFound("No se encontró el producto");
    }

    /// <summary>
    /// Agrega una opinión a un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    /// <param name="opinion">Opinión del usuario</param>
    [HttpPost("{id}/opinion")]
    public async Task<IActionResult> InsertOpinion(string id, [FromBody] OpinionModel opinion)
    {
        if (opinion.puntaje < 1 || opinion.puntaje > 5)
            return BadRequest("El puntaje debe ser un número entre 1 y 5");
        if (await Product.InsertOpinion(config, id, opinion))
            return Ok();
        return NotFound("No se encontró el producto");
    }
}
