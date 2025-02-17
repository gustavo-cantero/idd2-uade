using IDD2.Data;
using IDD2.Model;
using Microsoft.AspNetCore.Mvc;

namespace IDD2.Controllers;

/// <summary>
/// Controlador para la gestión de ventas
/// </summary>
/// <param name="config">Configuración de la aplicación</param>
[ApiController]
[Route("[controller]")]
public class SellController(ConfigurationModel config) : ControllerBase
{
    /// <summary>
    /// Convierte el carrito de compras en una venta
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Detalle de la venta</returns>
    [HttpGet("{userId}/confirm-cart")]
    public async Task<IActionResult> ConfirmCart(string userId)
    {
        var res = await Sell.ConfirmCart(config, userId);
        if (res == null)
            return Conflict("Verifique la información del carrito");
        return Ok(res);
    }

    /// <summary>
    /// Registra el pago de una venta
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="sellId">Identificador de la venta</param>
    /// <param name="payData">Datos del pago</param>
    /// <returns>Factura de la venta o <code>null</code> en caso de haber un error</returns>
    [HttpPost("{userId}/pay/{sellId}")]
    public async Task<IActionResult> Pay(string userId, string sellId, [FromBody] PagoModel payData)
    {
        var sell = await Sell.GetSell(config, userId, sellId);
        if (sell == null)
            return NotFound("Venta no encontrada");

        if (!string.IsNullOrWhiteSpace(sell.facturaId))
            return Conflict("La venta ya fue pagada y facturada");

        if (payData == null || string.IsNullOrWhiteSpace(payData.metodo))
            return BadRequest("Falta carga el método de pago");

        var invoice = await Sell.ApplyPay(config, sell, payData);
        if (invoice == null)
            return Conflict("No se pudo aplicar el pago");

        return Ok(invoice);
    }

    /// <summary>
    /// Devuelve la lista de ventas
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista paginada de ventas</returns>
    [HttpPost("{userId}/list")]
    public IActionResult List(string userId, [FromBody] FiltroVentasModel filter)
    {
        if (filter == null)
            return BadRequest("Los filtros son obligatorios");
        return Ok(Sell.List(config, userId, filter));
    }

    /// <summary>
    /// Devuelve la lista de facturas
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista paginada de facturas</returns>
    [HttpPost("{userId}/invoices")]
    public IActionResult ListInvoices(string userId, [FromBody] FiltroFacturasModel filter)
    {
        if (filter == null)
            return BadRequest("Los filtros son obligatorios");
        return Ok(Sell.ListInvoices(config, userId, filter));
    }

    /// <summary>
    /// Agrega los productos de una compra al carrito
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="sellId">Identificador de la venta</param>
    /// <returns>Resultado de la acción</returns>
    [HttpPost("{userId}/add-to-cart/{sellId}")]
    public async Task<IActionResult> AddToCart(string userId, string sellId) =>
        await Sell.AddToCart(config, userId, sellId) ? Ok() : NotFound();
}
