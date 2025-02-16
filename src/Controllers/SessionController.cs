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
    [HttpGet("{userId}/notifications/")]
    public async Task<IActionResult> Notifications(string userId) =>
        Ok(await Data.Session.ListNotifications(config, userId));

    /// <summary>
    /// Agrega una notificación
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    [HttpPost("{userId}/notifications")]
    public async Task<IActionResult> AddNotification(string userId, [FromForm] string notification)
    {
        await Data.Session.AddNotification(config, userId, notification);
        return Ok();
    }

    /// <summary>
    /// Elimina una notificación
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    [HttpDelete("{userId}/notifications")]
    public async Task<IActionResult> RemoveNotification(string userId, [FromForm] string notification)
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
    [HttpPost("{userId}/cart")]
    public async Task<IActionResult> SetProductToCart(string userId, [FromBody] ProductoEnCarritoModel datos)
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
    [HttpDelete("{userId}/cart/clean")]
    public async Task<IActionResult> ClearCart(string userId)
    {
        await Data.Session.ClearCart(config, userId);
        return Ok();
    }

    /// <summary>
    /// Elimina un producto del carrito de compras del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">Identificador del producto</param>
    [HttpDelete("{userId}/cart/{productId}")]
    public async Task<IActionResult> RemoveFromCart(string userId, string productId)
    {
        await Data.Session.RemoveProductFromCart(config, userId, productId);
        return Ok();
    }

    /// <summary>
    /// Devuelve el carrito de compras del usuario
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Datos del carrito de compras</returns>
    [HttpGet("{userId}/cart")]
    public async Task<IActionResult> GetCart(string userId) => Ok(await Data.Session.GetCart(config, userId));

    /// <summary>
    /// Valida el carrito de compras del usuario, comparando el stock con lo pedido y sin cambió algún precio
    /// </summary>
    /// <param name="config"></param>
    /// <param name="userId"></param>
    /// <returns>Si se precisó actualizar los datos del carrito, se devuelve el mismo</returns>
    [HttpGet("{userId}/cart/validate")]
    public async Task<IActionResult> ValidateCart(string userId) => Ok(await Data.Session.ValidateCart(config, userId));

    #endregion
}
