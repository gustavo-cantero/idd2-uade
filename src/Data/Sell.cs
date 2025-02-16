using IDD2.Model;
using MongoDB.Driver;

namespace IDD2.Data;

/// <summary>
/// Clase para la gestión de ventas y pagos
/// </summary>
public static class Sell
{
    private const string COLLECTION_SELLS = "ventas";
    private const string COLLECTION_INVOICES = "facturas";

    /// <summary>
    /// Convierte el carrito de compras en una venta
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Resultado de la conversón</returns>
    public static async Task<VentaModel?> ConfirmCart(ConfigurationModel config, string userId)
    {
        //Valido el carrito de compras
        if (await Session.ValidateCart(config, userId))
            return null;

        //Obtengo el carrito de compras
        var cart = await Session.GetCart(config, userId);
        if (!(cart?.Any() ?? false))
            return null;

        //Recorro los productos de la venta
        foreach (var p in cart)
        {
            //Decremento el stock del productos
            await Product.IncrementStock(config, p.productoId, -p.cantidad);
            //Registro la compra en neo4j
            await User.CreateRelation(config, userId, p.productoId, null);
        }

        //Creo la venta
        VentaModel sell = new()
        {
            usuarioId = userId,
            fecha = DateTime.UtcNow,
            total = cart.Sum(p => p.precio * p.cantidad),
            productos = cart.Select(p => new ProductoEnCarritoModel
            {
                productoId = p.productoId,
                cantidad = p.cantidad,
                precio = p.precio,
                nombre = p.nombre
            })
        };

        //Guardo la venta
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<VentaModel>(COLLECTION_SELLS); // nombre de la colección
        await collection.InsertOneAsync(sell);

        //Limpio el carrito de compras
        await Session.ClearCart(config, userId);

        return sell;
    }

    /// <summary>
    /// Obtiene los datos de una venta desde la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="sellId">Identificador de la venta</param>
    /// <returns>Datos del producto</returns>
    public static async Task<VentaModel> GetSell(ConfigurationModel config, string userId, string sellId)
    {
        //Valido el id de venta y el id de usuario, para evitar que un usuario vea las ventas de otro
        var filter = Builders<VentaModel>.Filter.Eq(p => p.id, sellId) & Builders<VentaModel>.Filter.Eq(p => p.usuarioId, userId);
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<VentaModel>(COLLECTION_SELLS); // nombre de la colección
        return await (await collection.FindAsync(filter)).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Aplica el pago en una venta y devuelve la factura
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="sell">Datos de la venta</param>
    /// <param name="payData">Datos del pago</param>
    /// <returns>Factura de la venta</returns>
    public static async Task<FacturaModel> ApplyPay(ConfigurationModel config, VentaModel sell, PagoModel payData)
    {
        payData.fecha = DateTime.UtcNow;

        //Busco la venta, verificando que no tenga una factura asociada, y guardo el método de pago
        var filter = Builders<VentaModel>.Filter.Eq(p => p.id, sell.id) & Builders<VentaModel>.Filter.Eq(p => p.facturaId, null);
        var update = Builders<VentaModel>.Update.Set(p => p.pago, payData);

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var sellsCol = database.GetCollection<VentaModel>(COLLECTION_SELLS);
        sell = await sellsCol.FindOneAndUpdateAsync(filter, update, new() { ReturnDocument = ReturnDocument.After });

        //Si no existe la venta, devuelvo null
        if (sell == null)
            return null;

        //Creo y guardo la factura
        FacturaModel invoice = new()
        {
            pago = payData,
            fecha = DateTime.UtcNow,
            numero = await InvoiceNumber(config), //Obtengo el siguiente número de factura
            sucursal = 1,
            //Genero un CAE ficticio
            cae = Random.Shared.Next(1000, 9999999).ToString().PadRight(7, '0') + Random.Shared.Next(1000, 9999999).ToString().PadRight(7, '0'),
            vtoCae = DateTime.UtcNow.AddDays(10),
            productos = sell.productos,
            total = sell.total
        };

        var invoicesCol = database.GetCollection<FacturaModel>(COLLECTION_INVOICES);
        await invoicesCol.InsertOneAsync(invoice);

        //Actualizo la venta para agregar la factura
        update = Builders<VentaModel>.Update.Set(p => p.facturaId, invoice.id);
        await sellsCol.FindOneAndUpdateAsync(filter, update);

        //Devuelvo la factura
        return invoice;
    }

    /// <summary>
    /// Obtiene el siguiente número de factura que está guardado en Redis
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <returns>Siguiente número de factura</returns>
    private static async Task<long> InvoiceNumber(ConfigurationModel config)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        return await connection.GetDatabase().StringIncrementAsync($"invoice", 1);
    }
}
