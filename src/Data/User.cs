using IDD2.Model;
using Neo4j.Driver;

namespace IDD2.Data;

public static class User
{
    /// <summary>
    /// Crea una relación entre el usuario y un producto
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">identificador del producto</param>
    /// <param name="ammount">Monto de la compra, o null si es una visita</param>
    public static async Task CreateRelation(ConfigurationModel config, string userId, string productId, double? ammount)
    {
        using var driver = Helpers.CreateNeo4jConnection(config);
        using var session = driver.AsyncSession();
        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                var query = @"
                    MERGE (u:Usuario {id: $userId})
                    MERGE (p:Producto {id: $productId})
                    MERGE (u)-[r:" + (ammount.HasValue ? "COMPRO" : "VISITO") + @"]->(p)
                    ON CREATE SET r.fecha = datetime($date), r.monto = $ammount";

                var parameters = new
                {
                    userId,
                    productId,
                    ammount,
                    date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                await tx.RunAsync(query, parameters);
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// Devuelve la categoría de un usuario según sus compras en los últimos 30 días
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Categoría del usuario</returns>
    public static async Task<string> GetCategory(ConfigurationModel config, string userId)
    {
        using var driver = Helpers.CreateNeo4jConnection(config);
        using var session = driver.AsyncSession();
        try
        {
            double res = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (:Usuario {id: $userId})-[r:COMPRO]->(:Producto)
                    WHERE r.fecha > datetime() - duration({days: 30})
                    RETURN COALESCE(SUM(r.monto), 0) AS total
                    ";
                var cursor = await tx.RunAsync(query, new { userId });
                var record = await cursor.SingleAsync();
                return Convert.ToDouble(record["total"]);
            });

            if (res > config.CategoriesLimits[0])
                return "TOP";
            if (res > config.CategoriesLimits[1])
                return "MEDIUM";
            return "LOW";
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}
