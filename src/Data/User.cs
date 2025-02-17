using IDD2.Model;
using MongoDB.Driver;
using Neo4j.Driver;

namespace IDD2.Data;

public static class User
{
    public const string COLLECTION = "usuarios";
    public const string USER_CAT_LOW = "LOW";
    public const string USER_CAT_MEDIUM = "MEDIUM";
    public const string USER_CAT_TOP = "TOP";

    /// <summary>
    /// Inserta un nuevo usuario en la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="user">Datos del usuario</param>
    public static async Task Insert(ConfigurationModel config, UsuarioModel user)
    {
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<UsuarioModel>(COLLECTION); // nombre de la colección
        user.fechaCreacion = DateTime.UtcNow;
        user.contraseña = Helpers.ComputeSHA256(user.contraseña);
        await collection.InsertOneAsync(user);
        user.contraseña = null; // No devuelvo la contraseña
    }

    /// <summary>
    /// Actualiza un usuario en la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="user">Datos del usuario</param>
    /// <returns>Datos actualizados del usuario</returns>
    public static async Task<UsuarioModel> Update(ConfigurationModel config, UsuarioModel user)
    {
        var filter = Builders<UsuarioModel>.Filter.Eq(p => p.id, user.id);
        var update = Builders<UsuarioModel>.Update
            .Set(p => p.fechaModificacion, DateTime.UtcNow)
            .Set(p => p.nombreApellido, user.nombreApellido)
            .Set(p => p.direccion, user.direccion)
            .Set(p => p.provincia, user.provincia)
            .Set(p => p.localidad, user.localidad)
            .Set(p => p.telefono, user.telefono);

        if (user.activo.HasValue)
            update = update.Set(p => p.activo, user.activo);
        if (!string.IsNullOrWhiteSpace(user.contraseña))
            update = update.Set(p => p.contraseña, Helpers.ComputeSHA256(user.contraseña));

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database);
        var collection = database.GetCollection<UsuarioModel>(COLLECTION);
        //Actualiza el documento y devuelve el usuario
        var res = await collection.FindOneAndUpdateAsync(filter, update, new() { ReturnDocument = ReturnDocument.After });
        res.contraseña = null;
        return res;
    }

    /// <summary>
    /// Obtiene un usuario de la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del usuario</param>
    /// <returns>Datos del usuario</returns>
    public static async Task<UsuarioModel> Get(ConfigurationModel config, string id)
    {
        var filter = Builders<UsuarioModel>.Filter.Eq(p => p.id, id);
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database);
        var collection = database.GetCollection<UsuarioModel>(COLLECTION);
        return await collection.Find(filter)
            .Project<UsuarioModel>(Builders<UsuarioModel>.Projection.Exclude(p => p.contraseña)) // Excluyo la contraseña
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Autentica un usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <returns>Resultado de la autenticación</returns>
    public static async Task<bool> Autenticar(ConfigurationModel config, string username, string password)
    {
        var filter = Builders<UsuarioModel>.Filter.Eq(p => p.nombre, username)
            & Builders<UsuarioModel>.Filter.Eq(p => p.contraseña, Helpers.ComputeSHA256(password));
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<UsuarioModel>(COLLECTION); // nombre de la colección
        return await (await collection.FindAsync(filter)).AnyAsync();
    }

    /// <summary>
    /// Marca un usuario como inactivo
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="userId">Identificador del usuario</param>
    /// <returns>Resultado del cambio</returns>
    public static async Task<bool> Delete(ConfigurationModel config, string userId)
    {
        var filter = Builders<UsuarioModel>.Filter.Eq(p => p.id, userId)
            & Builders<UsuarioModel>.Filter.Eq(p => p.activo, true);
        var update = Builders<UsuarioModel>.Update
            .Set(p => p.fechaModificacion, DateTime.UtcNow)
            .Set(p => p.activo, false);

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<UsuarioModel>(COLLECTION); // nombre de la colección
        //Actualiza el documento y devuelve el usuario actualizado
        return (await collection.UpdateOneAsync(filter, update)).ModifiedCount > 0;
    }

    /// <summary>
    /// Devuelve el nombre de un usuario
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del usuario</param>
    /// <returns>Nombre del usuario, o null en caso de no existir el producto</returns>
    public static async Task<string> GetName(ConfigurationModel config, string id)
    {
        var filter = Builders<UsuarioModel>.Filter.Eq(p => p.id, id);
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<UsuarioModel>(COLLECTION); // nombre de la colección
        return await collection.Find(filter).Project(p => p.nombre).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Crea una relación entre el usuario y un usero
    /// </summary>
    /// <param name="userId">Identificador del usuario</param>
    /// <param name="productId">identificador del producto</param>
    /// <param name="quantity">Cantidad de la compra, o null si es una visita</param>
    public static async Task<bool> CreateRelation(ConfigurationModel config, string userId, string productId, int? quantity)
    {
        using var driver = Helpers.CreateNeo4jConnection(config);
        using var session = driver.AsyncSession();
        try
        {
            return await session.ExecuteWriteAsync(async tx =>
            {
                string user = await GetName(config, userId);
                if (user == null)
                    return false;
                string product = await Product.GetName(config, productId);
                if (product == null)
                    return false;

                var query = @"
                    MERGE (u:Usuario {nombre: $user, id: $userId})
                    MERGE (p:Producto {nombre: $product, id: $productId})
                    CREATE (u)-[:" + (quantity.HasValue ? "COMPRO" : "VISITO") + " { fecha: datetime($date), cantidad: $quantity }]->(p)";

                var parameters = new
                {
                    userId,
                    productId,
                    quantity,
                    user,
                    product,
                    date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                await tx.RunAsync(query, parameters);
                return true;
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
                    RETURN COALESCE(SUM(r.cantidad), 0) AS total
                    ";
                var cursor = await tx.RunAsync(query, new { userId });
                var record = await cursor.SingleAsync();
                return Convert.ToDouble(record["total"]);
            });

            if (res > config.CategoriesLimits[0])
                return USER_CAT_TOP;
            if (res > config.CategoriesLimits[1])
                return USER_CAT_MEDIUM;
            return USER_CAT_LOW;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public static async Task<List<ActividadModel>> ListActivity(ConfigurationModel config, string userId, PaginadoModel paginado)
    {
        using var driver = Helpers.CreateNeo4jConnection(config);
        using var session = driver.AsyncSession();
        try
        {
            int offset = (paginado.pagina - 1) * paginado.elementosPorPagina; // Cálculo del offset

            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (:Usuario {id: $userId})-[r:VISITO|COMPRO]->(p:Producto)
                    RETURN p.nombre AS producto, p.id AS id, r.fecha AS fecha, type(r) AS accion, r.cantidad AS cantidad
                    ORDER BY r.fecha DESC
                    SKIP $offset
                    LIMIT $limit";

                var cursor = await tx.RunAsync(
                    query,
                    new
                    {
                        userId,
                        offset,
                        limit = paginado.elementosPorPagina
                    });

                List<ActividadModel> products = [];
                await cursor.ForEachAsync(record =>
                    products.Add(new()
                    {
                        producto = record["producto"].As<string>(),
                        id = record["id"].As<string>(),
                        fecha = record["fecha"].As<ZonedDateTime>().UtcDateTime,
                        accion = record["accion"].As<string>(),
                        cantidad = record["cantidad"].As<int?>()
                    }));

                return products;
            });

            return result;
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}
