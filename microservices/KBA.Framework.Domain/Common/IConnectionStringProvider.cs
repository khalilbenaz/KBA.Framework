namespace KBA.Framework.Domain.Common;

/// <summary>
/// Type d'opération de base de données
/// </summary>
public enum DatabaseOperationType
{
    Read,
    Write
}

/// <summary>
/// Interface pour fournir les chaînes de connexion selon le contexte
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Obtient la chaîne de connexion appropriée selon le type d'opération
    /// </summary>
    string GetConnectionString(DatabaseOperationType operationType = DatabaseOperationType.Write);

    /// <summary>
    /// Obtient une chaîne de connexion nommée spécifique
    /// </summary>
    string? GetNamedConnectionString(string name);
}

/// <summary>
/// Implémentation par défaut du fournisseur de chaînes de connexion
/// </summary>
public class DefaultConnectionStringProvider : IConnectionStringProvider
{
    private readonly ConnectionStringsOptions _options;

    public DefaultConnectionStringProvider(ConnectionStringsOptions options)
    {
        _options = options;
    }

    public string GetConnectionString(DatabaseOperationType operationType = DatabaseOperationType.Write)
    {
        return operationType switch
        {
            DatabaseOperationType.Read => _options.GetReadConnection(),
            DatabaseOperationType.Write => _options.GetWriteConnection(),
            _ => _options.GetWriteConnection()
        };
    }

    public string? GetNamedConnectionString(string name)
    {
        return _options.GetAdditionalConnection(name);
    }
}
