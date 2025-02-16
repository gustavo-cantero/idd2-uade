using IDD2.Model;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IDD2.Data;

/// <summary>
/// Clase encargada de administrar los productos
/// </summary>
public static class Product
{
    private const string COLLECTION = "productos";

    /// <summary>
    /// Inserta un nuevo producto en la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="product">Datos del producto</param>
    public static async Task Insert(ConfigurationModel config, ProductoModel product)
    {
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        product.fechaCreacion = DateTime.UtcNow;
        await collection.InsertOneAsync(product);
    }

    /// <summary>
    /// Actualiza un producto en la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="product">Datos del producto</param>
    /// <returns>Datos del producto actualizado</returns>
    public static async Task<ProductoModel> Update(ConfigurationModel config, ProductoModel product)
    {
        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, product.id);
        var update = Builders<ProductoModel>.Update
            .Set(p => p.fechaModificacion, DateTime.UtcNow)
            .Set(p => p.precio, product.precio)
            .Set(p => p.stock, product.stock)
            .Set(p => p.nombre, product.nombre)
            .Set(p => p.descripcion, product.descripcion)
            .Set(p => p.categoria, product.categoria)
            .Set(p => p.codigoDeBarras, product.codigoDeBarras);

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        //Actualiza el documento y devuelve el producto actualizado
        return await collection.FindOneAndUpdateAsync(filter, update, new() { ReturnDocument = ReturnDocument.After });
    }

    /// <summary>
    /// Establece el precio de un producto
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del producto</param>
    /// <param name="price">Nuevo precio</param>
    /// <returns>Resultado de la grabación</returns>
    public static async Task<bool> SetPrice(ConfigurationModel config, string id, double price)
    {
        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, id);
        var update = Builders<ProductoModel>.Update
            .Set(p => p.precio, price)
            .Set(p => p.fechaModificacion, DateTime.UtcNow);

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        //Actualiza el documento y devuelve el producto actualizado
        return (await collection.UpdateOneAsync(filter, update)).MatchedCount > 0;
    }

    /// <summary>
    /// Elimina un producto de la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del producto</param>
    /// <returns>Resultado de la eliminación</returns>
    public static async Task<bool> Delete(ConfigurationModel config, string id)
    {
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección

        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, id);
        var resultado = await collection.DeleteOneAsync(filter);

        return resultado.DeletedCount > 0; // Devuelve true si eliminó al menos un documento
    }

    /// <summary>
    /// Obtiene un producto de la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del producto</param>
    /// <returns>Datos del producto</returns>
    public static async Task<ProductoModel> Get(ConfigurationModel config, string id)
    {
        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, id);
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        return await (await collection.FindAsync(filter)).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Determina si un producto existe en la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del producto</param>
    /// <returns>Devuelve si existe un producto</returns>
    public static async Task<bool> Exists(ConfigurationModel config, string id)
    {
        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, id);
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        return await (await collection.FindAsync(filter)).AnyAsync();
    }

    /// <summary>
    /// Inserta una opinión en un producto
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="id">Identificador del producto</param>
    /// <param name="opinion">Datos de la opinión</param>
    /// <returns>Resultado de la inserción</returns>
    public static async Task<bool> InsertOpinion(ConfigurationModel config, string id, OpinionModel opinion)
    {
        var filter = Builders<ProductoModel>.Filter.Eq(p => p.id, id);

        var update = Builders<ProductoModel>.Update
            .Push(p => p.opiniones, opinion);

        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección

        //Grabo la nueva opinión en el producto
        var res = await collection.UpdateOneAsync(filter, update);

        if (res.ModifiedCount == 0)
            return false;

        //Calculo el promedio de los puntajes de las opiniones
        var product = await collection.Find(filter).FirstOrDefaultAsync();
        var avg = product.opiniones.Average(opinion => opinion.puntaje);
        var updateScore = Builders<ProductoModel>.Update.Set(p => p.puntaje, avg);
        await collection.UpdateOneAsync(filter, updateScore);

        return true; // Devuelve true si se modificó el documento
    }

    /// <summary>
    /// Lista los productos de la base de datos
    /// </summary>
    /// <param name="config">Configuración de la aplicación</param>
    /// <param name="filtro">Filtro para aplicar en la búsqueda</param>
    /// <returns>Listado de productos encontrados</returns>
    public static List<ProductoModel> List(ConfigurationModel config, FiltroProductos filtro)
    {
        // Crear el filtro base
        var mongoFiltro = Builders<ProductoModel>.Filter.Empty; // Filtro vacío por defecto

        // Filtrar por nombre (si es proporcionado)
        if (!string.IsNullOrEmpty(filtro.nombre))
            mongoFiltro &= Builders<ProductoModel>.Filter.Regex(p => p.nombre, new BsonRegularExpression(filtro.nombre, "i"));

        // Filtrar por precio (rango)
        if (filtro.precioMin.HasValue)
            mongoFiltro &= Builders<ProductoModel>.Filter.Gte(p => p.precio, filtro.precioMin.Value);
        if (filtro.precioMax.HasValue)
            mongoFiltro &= Builders<ProductoModel>.Filter.Lte(p => p.precio, filtro.precioMax.Value);

        // Filtrar por código de barras
        if (!string.IsNullOrEmpty(filtro.codigoDeBarras))
            mongoFiltro &= Builders<ProductoModel>.Filter.Eq(p => p.codigoDeBarras, filtro.codigoDeBarras);

        // Filtrar por categoría
        if (!string.IsNullOrEmpty(filtro.categoria))
            mongoFiltro &= Builders<ProductoModel>.Filter.Eq(p => p.categoria, filtro.categoria);

        // Filtrar por puntaje (rango)
        if (filtro.puntajeMin.HasValue)
            mongoFiltro &= Builders<ProductoModel>.Filter.Gte(p => p.puntaje, filtro.puntajeMin.Value);

        // Definir el orden
        SortDefinition<ProductoModel> orden = Builders<ProductoModel>.Sort.Ascending(p => p.nombre); // Orden por defecto: Ascendente por nombre

        if (filtro.ordenPor != null)
        {
            orden = filtro.ordenPor.ToLower() switch
            {
                "precio" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<ProductoModel>.Sort.Descending(p => p.precio)
                                            : Builders<ProductoModel>.Sort.Ascending(p => p.precio),
                "puntaje" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<ProductoModel>.Sort.Descending(p => p.puntaje)
                                            : Builders<ProductoModel>.Sort.Ascending(p => p.puntaje),
                "nombre" => filtro.ordenDireccion.ToLower() == "desc"
                                            ? Builders<ProductoModel>.Sort.Descending(p => p.nombre)
                                            : Builders<ProductoModel>.Sort.Ascending(p => p.nombre),
                _ => Builders<ProductoModel>.Sort.Ascending(p => p.nombre),// Si no se encuentra el campo, ordenar por nombre por defecto
            };
        }

        // Realizo la paginación y ordenamiento
        using var client = Helpers.CreateMongoDBConnection(config);
        var database = client.GetDatabase(config.MongoDB.Database); // nombre de la base de datos
        var collection = database.GetCollection<ProductoModel>(COLLECTION); // nombre de la colección
        var productos = collection.Find(mongoFiltro)
                                    .Project<ProductoModel>(Builders<ProductoModel>.Projection.Exclude(p => p.opiniones)) // Excluir 'Opiniones'
                                    .Sort(orden)
                                    .Skip((filtro.pagina - 1) * filtro.elementosPorPagina)
                                    .Limit(filtro.elementosPorPagina)
                                    .ToList();

        return productos;
    }
}
