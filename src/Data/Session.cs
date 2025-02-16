using IDD2.Model;
using System.Text.Json;

namespace IDD2.Data;

/// <summary>
/// Mantiene los datos de la sesión del usuario
/// </summary>
public static class Session
{
    /// <summary>
    /// Agrega una notificación
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    public static async Task AddNotification(ConfigurationModel config, int userId, string notification)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        var db = connection.GetDatabase();
        await db.ListRemoveAsync($"u{userId}.notif", notification);
        await db.ListLeftPushAsync($"u{userId}.notif", notification);
    }

    /// <summary>
    /// Elimina una notificación del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="notification">Notificación</param>
    public static async Task RemoveNotification(ConfigurationModel config, int userId, string notification)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        await connection.GetDatabase().ListRemoveAsync($"u{userId}.notif", notification);
    }

    /// <summary>
    /// Devuelve las notificaciones del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Notificaciones del usuario</returns>
    public static async Task<IEnumerable<string>> ListNotifications(ConfigurationModel config, int userId)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        return (await connection.GetDatabase().ListRangeAsync($"u{userId}.notif")).Select(x => x.ToString());
    }

    /// <summary>
    /// Establece la cantidad de un producto en el carrito
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="datos">Datos del producto</param>
    public static async Task SetProductToCart(ConfigurationModel config, int userId, ProductoEnCarritoModel datos)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        var db = connection.GetDatabase();
        if (datos.cantidad == 0)
            await db.HashDeleteAsync($"u{userId}.cart", datos.productoId);
        else
            await db.HashSetAsync($"u{userId}.cart", datos.productoId, JsonSerializer.Serialize(datos));
    }

    /// <summary>
    /// Elimina un producto del carrito de compras del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">Identificador del producto</param>
    public static async Task RemoveProductFromCart(ConfigurationModel config, int userId, string productId)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        await connection.GetDatabase().HashDeleteAsync($"u{userId}.cart", productId);
    }

    /// <summary>
    /// Limpia el carrito de compras del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    public static async Task ClearCart(ConfigurationModel config, int userId)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        await connection.GetDatabase().KeyDeleteAsync($"u{userId}.cart");
    }

    /// <summary>
    /// Devuelve el carrito de compras del usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Datos del carrito de compras</returns>
    public static async Task<IEnumerable<ProductoEnCarritoModel?>> GetCart(ConfigurationModel config, int userId)
    {
        using var connection = Helpers.CreateRedisConnection(config);
        var productos = await connection.GetDatabase().HashGetAllAsync($"u{userId}.cart");
        return productos.Select(x => JsonSerializer.Deserialize<ProductoEnCarritoModel>(x.Value!));
    }
}
