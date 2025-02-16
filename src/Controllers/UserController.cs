using IDD2.Model;
using Microsoft.AspNetCore.Mvc;

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
    /// Registra la visita de un usuario a un producto 
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">Identificador del producto</param>
    [HttpPost("{userId:int}/visit/{productId}")]
    public async Task<IActionResult> Post(string userId, string productId)
    {
        await Data.User.CreateRelation(config, userId, productId, null);
        return Ok();
    }

    /// <summary>
    /// Devuelve la categoría de un usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Categoría del usuario</returns>
    [HttpGet("{userId:int}/category")]
    public async Task<IActionResult> Category(string userId) =>
        Ok(await Data.User.GetCategory(config, userId));
}
