namespace IDD2.Model;

/// <summary>
/// Configuración de la aplicación
/// </summary>
public class ConfigurationModel
{
    public Neo4jConfiguration Neo4j { get; set; }
    public RedisConfiguration Redis { get; set; }
    public MongoDBConfiguration MongoDB { get; set; }
    public double[] CategoriesLimits { get; set; }
}

public class Neo4jConfiguration
{
    public string Uri { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
}

public class MongoDBConfiguration
{
    public string ConnectionString { get; set; }
    public string Database { get; set; }
}