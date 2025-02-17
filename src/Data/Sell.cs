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

        //Recorro los ventas de la venta
        foreach (var p in cart)
        {
            //Decremento el stock del ventas
            await Product.IncrementStock(config, p.productoId, -p.cantidad);
            //Registro la compra en neo4j
            await User.CreateRelation(config, userId, p.productoId, p.cantidad);
        }

        //Creo la venta
        double totNoTax = cart.Sum(p => p.precio * p.cantidad);
        var userCat = await User.GetCategory(config, userId);
        double discount = userCat switch
        {
            User.USER_CAT_MEDIUM => Math.Round(totNoTax * .05, 2),
            User.USER_CAT_TOP => Math.Round(totNoTax * .1, 2),
            _ => 0,
        };

        VentaModel sell = new()
        {
            usuario = await User.Get(config, userId), //Obtengo el usuario
            fecha = DateTime.UtcNow,
            totalSinImpuestos = totNoTax,
            impuestos = Math.Round(totNoTax * .21, 2),
            descuentos = discount,
            total = Math.Round(totNoTax * 1.21, 2) - discount,
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
        var filter = Builders<VentaModel>.Filter.Eq(p => p.id, sellId) & Builders<VentaModel>.Filter.Eq(p => p.usuario.id, userId);
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

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var sellsCol = database.GetCollection<VentaModel>(COLLECTION_SELLS);
        sell = await sellsCol.Find(filter).FirstOrDefaultAsync();

        //Si no existe la venta, devuelvo null
        if (sell == null)
            return null;

        //Creo y guardo la factura
        FacturaModel invoice = new()
        {
            venta = sell,
            pago = payData,
            fecha = DateTime.UtcNow,
            numero = await InvoiceNumber(config), //Obtengo el siguiente número de factura
            sucursal = 1,
            //Genero un CAE ficticio
            cae = Random.Shared.Next(1000, 9999999).ToString().PadRight(7, '0') + Random.Shared.Next(1000, 9999999).ToString().PadRight(7, '0'),
            vtoCae = DateTime.UtcNow.AddDays(10)
        };

        var invoicesCol = database.GetCollection<FacturaModel>(COLLECTION_INVOICES);
        await invoicesCol.InsertOneAsync(invoice);

        //Actualizo la venta para agregar la factura
        var update = Builders<VentaModel>.Update.Set(p => p.facturaId, invoice.id);
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

    /// <summary>
    /// Lista de compras realizadas por un usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="filtro">Filtro para aplicar en la búsqueda</param>
    /// <returns>Listado de ventas encontradas en la base de datos</returns>
    public static List<VentaModel> List(ConfigurationModel config, string userId, FiltroVentasModel filtro)
    {
        // Crear el filtro base
        var mongoFiltro = Builders<VentaModel>.Filter.Eq(p => p.usuario.id, userId);

        // Filtrar por monto total (rango)
        if (filtro.totalMin.HasValue)
            mongoFiltro &= Builders<VentaModel>.Filter.Gte(p => p.total, filtro.totalMin.Value);
        if (filtro.totalMax.HasValue)
            mongoFiltro &= Builders<VentaModel>.Filter.Lte(p => p.total, filtro.totalMax.Value);

        // Filtrar por fecha (rango)
        if (filtro.fechaDesde.HasValue)
            mongoFiltro &= Builders<VentaModel>.Filter.Gte(p => p.fecha, filtro.fechaDesde.Value);
        if (filtro.fechaHasta.HasValue)
            mongoFiltro &= Builders<VentaModel>.Filter.Lte(p => p.fecha, filtro.fechaHasta.Value);

        // Filtrar por pagada o no
        if (filtro.pagada.HasValue)
            if (filtro.pagada.Value)
                mongoFiltro &= Builders<VentaModel>.Filter.Ne(p => p.facturaId, null);
            else
                mongoFiltro &= Builders<VentaModel>.Filter.Eq(p => p.facturaId, null);

        // Definir el orden
        SortDefinition<VentaModel> orden = Builders<VentaModel>.Sort.Descending(p => p.fecha);

        if (filtro.ordenPor != null)
        {
            orden = filtro.ordenPor.ToLower() switch
            {
                "fecha" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<VentaModel>.Sort.Descending(p => p.fecha)
                                            : Builders<VentaModel>.Sort.Ascending(p => p.fecha),
                "total" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<VentaModel>.Sort.Descending(p => p.total)
                                            : Builders<VentaModel>.Sort.Ascending(p => p.total),
                _ => Builders<VentaModel>.Sort.Descending(p => p.fecha)
            };
        }

        // Realizo la paginación y ordenamiento
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<VentaModel>(COLLECTION_SELLS); // nombre de la colección
        return collection.Find(mongoFiltro)
                         .Sort(orden)
                         .Skip((filtro.pagina - 1) * filtro.elementosPorPagina)
                         .Limit(filtro.elementosPorPagina)
                         .ToList();
    }

    /// <summary>
    /// Lista de facturas de un usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="filtro">Filtro para aplicar en la búsqueda</param>
    /// <returns>Listado de facturas encontradas en la base de datos</returns>
    public static List<FacturaModel> ListInvoices(ConfigurationModel config, string userId, FiltroFacturasModel filtro)
    {
        // Crear el filtro base
        var mongoFiltro = Builders<FacturaModel>.Filter.Eq(p => p.venta.usuario.id, userId);

        // Filtrar por número de factura (rango)
        if (filtro.numeroMin.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Gte(p => p.numero, filtro.numeroMin.Value);
        if (filtro.numeroMax.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Lte(p => p.numero, filtro.numeroMax.Value);

        // Filtrar por monto total (rango)
        if (filtro.totalMin.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Gte(p => p.venta.total, filtro.totalMin.Value);
        if (filtro.totalMax.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Lte(p => p.venta.total, filtro.totalMax.Value);

        // Filtrar por fecha (rango)
        if (filtro.fechaDesde.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Gte(p => p.fecha, filtro.fechaDesde.Value);
        if (filtro.fechaHasta.HasValue)
            mongoFiltro &= Builders<FacturaModel>.Filter.Lte(p => p.fecha, filtro.fechaHasta.Value);

        // Definir el orden
        SortDefinition<FacturaModel> orden = Builders<FacturaModel>.Sort.Descending(p => p.fecha);

        if (filtro.ordenPor != null)
        {
            orden = filtro.ordenPor.ToLower() switch
            {
                "fecha" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<FacturaModel>.Sort.Descending(p => p.fecha)
                                            : Builders<FacturaModel>.Sort.Ascending(p => p.fecha),
                "total" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<FacturaModel>.Sort.Descending(p => p.venta.total)
                                            : Builders<FacturaModel>.Sort.Ascending(p => p.venta.total),
                _ => Builders<FacturaModel>.Sort.Descending(p => p.fecha)
            };
        }

        // Realizo la paginación y ordenamiento
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<FacturaModel>(COLLECTION_INVOICES); // nombre de la colección
        return collection.Find(mongoFiltro)
                         .Sort(orden)
                         .Skip((filtro.pagina - 1) * filtro.elementosPorPagina)
                         .Limit(filtro.elementosPorPagina)
                         .ToList();
    }

    /// <summary>
    /// Agrega los productos de una compra al carrito
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="sellId">Identificador de la venta</param>
    /// <returns>Resultado de la acción</returns>
    public static async Task<bool> AddToCart(ConfigurationModel config, string userId, string sellId)
    {
        var sell = await GetSell(config, userId, sellId);
        if (sell == null)
            return false;

        foreach (var p in sell.productos)
            await Session.SetProductToCart(config, userId, new ProductoEnCarritoModel
            {
                productoId = p.productoId,
                cantidad = p.cantidad,
                precio = p.precio,
                nombre = p.nombre
            });

        await Session.ValidateCart(config, userId);
        return true;
    }
}
