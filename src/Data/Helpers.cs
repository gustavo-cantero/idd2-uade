using IDD2.Model;
using MongoDB.Driver;
using Neo4j.Driver;
using StackExchange.Redis;

namespace IDD2.Data;

/// <summary>
/// Clase de ayuda para la conexión a la base de datos
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Crea una conexión a la base de datos de Neo4j
    /// </summary>
    /// <param name="config">Configuración de la base de datos</param>
    /// <returns>Conexión a la base de datos de Neo4j</returns>
    public static IDriver CreateNeo4jConnection(ConfigurationModel config) =>
        GraphDatabase.Driver(config.Neo4j.Uri, AuthTokens.Basic(config.Neo4j.User, config.Neo4j.Password));

    /// <summary>
    /// Crea una conexión a la base de datos de Redis
    /// </summary>
    /// <param name="config">Configuración de la base de datos</param>
    /// <returns>Conexión a la base de datos de Redis</returns>
    public static ConnectionMultiplexer CreateRedisConnection(ConfigurationModel config) =>
        ConnectionMultiplexer.Connect(config.Redis.ConnectionString);

    /// <summary>
    /// Crea una conexión a la base de datos de Redis
    /// </summary>
    /// <param name="config">Configuración de la base de datos</param>
    /// <returns>Conexión a la base de datos de Redis</returns>
    public static MongoClient CreateMongoDBConnection(ConfigurationModel config) =>
        new(config.MongoDB.ConnectionString);
}
