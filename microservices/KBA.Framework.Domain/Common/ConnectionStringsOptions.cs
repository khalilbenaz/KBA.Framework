namespace KBA.Framework.Domain.Common;

/// <summary>
/// Configuration pour plusieurs chaînes de connexion
/// </summary>
public class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// Chaîne de connexion par défaut (pour compatibilité)
    /// </summary>
    public string DefaultConnection { get; set; } = string.Empty;

    /// <summary>
    /// Chaîne de connexion principale (écriture)
    /// </summary>
    public string WriteConnection { get; set; } = string.Empty;

    /// <summary>
    /// Chaîne de connexion pour la lecture (Read Replica)
    /// </summary>
    public string ReadConnection { get; set; } = string.Empty;

    /// <summary>
    /// Chaînes de connexion additionnelles pour des besoins spécifiques
    /// </summary>
    public Dictionary<string, string> Additional { get; set; } = new();

    /// <summary>
    /// Obtient la chaîne de connexion d'écriture
    /// </summary>
    public string GetWriteConnection()
    {
        return !string.IsNullOrEmpty(WriteConnection) ? WriteConnection : DefaultConnection;
    }

    /// <summary>
    /// Obtient la chaîne de connexion de lecture
    /// </summary>
    public string GetReadConnection()
    {
        return !string.IsNullOrEmpty(ReadConnection) ? ReadConnection : GetWriteConnection();
    }

    /// <summary>
    /// Obtient une chaîne de connexion additionnelle par nom
    /// </summary>
    public string? GetAdditionalConnection(string name)
    {
        return Additional.TryGetValue(name, out var connection) ? connection : null;
    }
}
