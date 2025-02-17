using IDD2.Model;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace IDD2.Controllers;

/// <summary>
/// Controlador para la gestión de usuarios
/// </summary>
/// <param name="config"></param>
[ApiController]
[Route("[controller]")]
public class UserController(ConfigurationModel config) : ControllerBase
{
    /// <summary>
    /// Inserta un usuario
    /// </summary>
    /// <param name="user">Datos del usuario</param>
    /// <returns>Detalle del usuario insertado</returns>
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] UsuarioModel user)
    {
        try
        {
            await Data.User.Insert(config, user);
            return Ok(user);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict("Ya existe otro usuario con ese nombre");
        }
    }

    /// <summary>
    /// Actualiza un usuario
    /// </summary>
    /// <param name="id">Identificador del usuario</param>
    /// <param name="user">Datos del usuario</param>
    /// <returns>Detalle del usuario insertado</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UsuarioModel user)
    {
        try
        {
            user.id = id;
            var res = await Data.User.Update(config, user);
            if (res == null)
                return NotFound("No se encontró el usuario");
            return Ok(res);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            return Conflict("Ya existe otro usuario con ese nombre");
        }
    }

    /// <summary>
    /// Elimina un producto
    /// </summary>
    /// <param name="id">Identificador del producto</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (await Data.User.Delete(config, id))
            return Ok();
        return NotFound("No se encontró el usuario activo");
    }

    /// <summary>
    /// Registra la visita de un usuario a un producto 
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">Identificador del producto</param>
    [HttpGet("{userId}/visit/{productId}")]
    public async Task<IActionResult> Visit(string userId, string productId)
    {
        if (await Data.User.CreateRelation(config, userId, productId, null))
            return Ok();
        return NotFound();
    }

    /// <summary>
    /// Devuelve la categoría de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Categoría del usuario</returns>
    [HttpGet("{userId}/category")]
    public async Task<IActionResult> Category(string userId) =>
        Ok(await Data.User.GetCategory(config, userId));

    /// <summary>
    /// Devuelve la actividad del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="pag">Datos para el paginado</param>
    /// <returns>Actividad del usuario</returns>
    [HttpPost("{userId}/activity")]
    public async Task<IActionResult> Activity(string userId, [FromBody] PaginadoModel pag) =>
        Ok(await Data.User.ListActivity(config, userId, pag));

    /// <summary>
    /// Autentica un usuario
    /// </summary>
    /// <param name="datos">Datos para autenticar</param>
    /// <returns>Resultado de la autenticación</returns>
    [HttpPost("auth")]
    public async Task<IActionResult> Auth([FromBody] AutenticacionModel datos)
    {
        if (await Data.User.Autenticar(config, datos.nombre, datos.contraseña))
            return Ok();
        return StatusCode(403); //Forbiden
    }


}
