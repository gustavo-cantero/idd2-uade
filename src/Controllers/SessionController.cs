using IDD2.Model;
using Microsoft.AspNetCore.Mvc;

namespace IDD2.Controllers;

[ApiController]
[Route("[controller]")]
public class SessionController(ConfigurationModel config) : ControllerBase
{
    #region Notificaciones

    /// <summary>
    /// Devuelve las notificaciones del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Notificaciones del usuario</returns>
    [HttpGet("{userId:int}/notifications/")]
    public async Task<IActionResult> Notifications(int userId) =>
        Ok(await Data.Session.ListNotifications(config, userId));

    /// <summary>
    /// Agrega una notificación
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    [HttpPost("{userId:int}/notifications")]
    public async Task<IActionResult> AddNotification(int userId, [FromForm] string notification)
    {
        await Data.Session.AddNotification(config, userId, notification);
        return Ok();
    }

    /// <summary>
    /// Elimina una notificación
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    [HttpDelete("{userId:int}/notifications")]
    public async Task<IActionResult> RemoveNotification(int userId, [FromForm] string notification)
    {
        await Data.Session.RemoveNotification(config, userId, notification);
        return Ok();
    }

    #endregion

    #region Carrito de compras

    /// <summary>
    /// Establece la cantidad de un producto en el carrito
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="datos">Datos del producto</param>
    [HttpPost("{userId:int}/cart")]
    public async Task<IActionResult> SetProductToCart(int userId, [FromBody] ProductoEnCarritoModel datos)
    {
        if (string.IsNullOrWhiteSpace(datos.productoId) || datos.precio <= 0 || datos.cantidad < 0)
            return BadRequest();
        if (!await Data.Product.Exists(config, datos.productoId)) //Verifico que exista el producto
            return NotFound();
        await Data.Session.SetProductToCart(config, userId, datos);
        return Ok();
    }

    /// <summary>
    /// Limpia el carrito de compras del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    [HttpDelete("{userId:int}/cart/clean")]
    public async Task<IActionResult> ClearCart(int userId)
    {
        await Data.Session.ClearCart(config, userId);
        return Ok();
    }

    /// <summary>
    /// Elimina un producto del carrito de compras del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">Identificador del producto</param>
    [HttpDelete("{userId:int}/cart/{productId}")]
    public async Task<IActionResult> RemoveFromCart(int userId, string productId)
    {
        await Data.Session.RemoveProductFromCart(config, userId, productId);
        return Ok();
    }

    /// <summary>
    /// Devuelve el carrito de compras del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Datos del carrito de compras</returns>
    [HttpGet("{userId:int}/cart")]
    public async Task<IActionResult> GetCart(int userId) => Ok(await Data.Session.GetCart(config, userId));

    #endregion
}
